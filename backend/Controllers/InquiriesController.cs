using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InquiriesController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        // GET: api/Inquiries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inquiry>>> GetUserInquiries()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<Inquiry> inquiries = await _context.Inquiries
                .Where(i => i.UserId == userId)
                .Include(i => i.Vehicle)
                .OrderByDescending(i => i.DateSent)
                .ToListAsync();

            return inquiries;
        }

        // GET: api/Inquiries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inquiry>> GetInquiry(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            Inquiry? inquiry = await _context.Inquiries
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            return inquiry == null ? (ActionResult<Inquiry>)NotFound() : (ActionResult<Inquiry>)inquiry;
        }

        // POST: api/Inquiries
        [HttpPost]
        public async Task<ActionResult<Inquiry>> CreateInquiry(InquiryCreateDto inquiryDto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if vehicle exists
            Vehicle? vehicle = await _context.Vehicles.FindAsync(inquiryDto.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { Message = "Vehicle not found" });
            }

            Inquiry inquiry = new Inquiry
            {
                UserId = userId,
                VehicleId = inquiryDto.VehicleId,
                Subject = inquiryDto.Subject,
                Message = inquiryDto.Message,
                DateSent = DateTime.Now,
                Status = InquiryStatus.New,
            };

            _ = _context.Inquiries.Add(inquiry);
            _ = await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInquiry), new { id = inquiry.Id }, inquiry);
        }

        // PUT: api/Inquiries/5/MarkAsRead
        [HttpPut("{id}/MarkAsRead")]
        [Authorize(Roles = "Admin")] // For admin access only
        public async Task<IActionResult> MarkInquiryAsRead(int id)
        {
            Inquiry? inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.Status = InquiryStatus.Read;
            _context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquiryExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        // PUT: api/Inquiries/5/Reply
        [HttpPut("{id}/Reply")]
        [Authorize(Roles = "Admin")] // For admin access only
        public async Task<IActionResult> ReplyToInquiry(int id, InquiryReplyDto replyDto)
        {
            Inquiry? inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.Response = replyDto.Response;
            inquiry.DateReplied = DateTime.Now;
            inquiry.Status = InquiryStatus.Replied;

            _context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquiryExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        // PUT: api/Inquiries/5/Close
        [HttpPut("{id}/Close")]
        public async Task<IActionResult> CloseInquiry(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            Inquiry? inquiry = await _context.Inquiries
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.Status = InquiryStatus.Closed;
            _context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquiryExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        private bool InquiryExists(int id)
        {
            return _context.Inquiries.Any(e => e.Id == id);
        }
    }

    public class InquiryCreateDto
    {
        public int VehicleId { get; set; }

        public string? Subject { get; set; }

        public string? Message { get; set; }
    }

    public class InquiryReplyDto
    {
        public string? Response { get; set; }
    }
}