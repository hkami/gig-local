﻿namespace GigLocal.Pages.Admin.Gigs;

public class CreateModel : PageModel
{
    private readonly GigContext _context;
    private readonly IImageService _imageService;

    [BindProperty]
    public GigCreateModel Gig { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public IEnumerable<SelectListItem> Venues { get; set; }

    public CreateModel(
        GigContext context,
        IImageService storageService,
        IRecaptchaService recaptchaService)
    {
        _context = context;
        _imageService = storageService;
    }

    public async Task<IActionResult> OnGetAsync(int? templateId)
    {
        Venues = await VenueHelper.GetSelectListAsync(_context);

        if (templateId != null)
        {
            Gig gig = await _context.Gigs
                .AsNoTracking()
                .Include(g => g.Venue)
                .FirstOrDefaultAsync(m => m.ID == templateId);

            if (gig == null)
            {
                return NotFound();
            }

            Gig = new GigCreateModel
            {
                ArtistName = gig.ArtistName,
                VenueID = gig.VenueID.ToString(),
                Date = gig.Date,
                Description = gig.Description,
                EventUrl = gig.EventUrl
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Venues = await VenueHelper.GetSelectListAsync(_context);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var foundVenue = await _context.Venues
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(v => v.ID == int.Parse(Gig.VenueID));
        if (foundVenue == null)
        {
            return NotFound();
        }

        using var imageStream = Gig.FormFile.OpenReadStream();
        var imageUrl = await _imageService.UploadImageAsync(imageStream);

        var newGig = new Gig
        {
            VenueID = foundVenue.ID,
            Date = Gig.Date,
            ArtistName = Gig.ArtistName,
            Description = Gig.Description,
            EventUrl = Gig.EventUrl,
            ImageUrl = imageUrl,
            Approved = true
        };

        _context.Gigs.Add(newGig);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}

public class GigCreateModel
{
    [Required]
    [Display(Name = "Venue")]
    public string VenueID { get; set; }

    [Required]
    [FutureDate(ErrorMessage = "The date must be in the future.")]
    [Display(Name = "Date and time")]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Artist name")]
    public string ArtistName { get; set; }

    [Required]
    [MaxLength(300)]
    public string Description { get; set; }

    [Required]
    [Display(Name = "Event URL")]
    public string EventUrl { get; set; }

    [Required]
    [Display(Name = "Image")]
    public IFormFile FormFile { get; set; }
}
