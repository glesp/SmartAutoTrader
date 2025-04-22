namespace SmartAutoTrader.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.DTOs;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Helpers;
    using SmartAutoTrader.API.Models;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InquiriesController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext context = context;

        // GET: api/Inquiries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inquiry>>> GetUserInquiries()
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            List<Inquiry> inquiries = await this.context.Inquiries
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
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            Inquiry? inquiry = await this.context.Inquiries
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            return inquiry == null ? (ActionResult<Inquiry>)this.NotFound() : (ActionResult<Inquiry>)inquiry;
        }

        // GET: api/Inquiries/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Inquiry>>> GetAllInquiries([FromQuery] string status = "")
        {
            IQueryable<Inquiry> query = this.context.Inquiries
                .Include(i => i.Vehicle)
                .Include(i => i.User)
                .OrderByDescending(i => i.DateSent);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InquiryStatus>(status, out var inquiryStatus))
            {
                query = query.Where(i => i.Status == inquiryStatus);
            }

            List<Inquiry> inquiries = await query.ToListAsync();
            return inquiries;
        }

        // POST: api/Inquiries
        [HttpPost]
        public async Task<ActionResult<Inquiry>> CreateInquiry(InquiryCreateDto inquiryDto)
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            // Check if vehicle exists
            Vehicle? vehicle = await this.context.Vehicles.FindAsync(inquiryDto.VehicleId);
            if (vehicle == null)
            {
                return this.NotFound(new { Message = "Vehicle not found" });
            }

            Inquiry inquiry = new()
            {
                UserId = userId.Value,
                VehicleId = inquiryDto.VehicleId,
                Subject = inquiryDto.Subject,
                Message = inquiryDto.Message,
                DateSent = DateTime.Now,
                Status = InquiryStatus.New,
            };

            _ = this.context.Inquiries.Add(inquiry);
            _ = await this.context.SaveChangesAsync();

            return this.CreatedAtAction(nameof(this.GetInquiry), new { id = inquiry.Id }, inquiry);
        }

        // PUT: api/Inquiries/5/MarkAsRead
        [HttpPut("{id}/MarkAsRead")]
        [Authorize(Roles = "Admin")] // For admin access only
        public async Task<IActionResult> MarkInquiryAsRead(int id)
        {
            Inquiry? inquiry = await this.context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return this.NotFound();
            }

            inquiry.Status = InquiryStatus.Read;
            this.context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await this.context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.InquiryExists(id))
                {
                    return this.NotFound();
                }

                throw;
            }

            return this.NoContent();
        }

        // PUT: api/Inquiries/5/Reply
        [HttpPut("{id}/Reply")]
        [Authorize(Roles = "Admin")] // For admin access only
        public async Task<IActionResult> ReplyToInquiry(int id, InquiryReplyDto replyDto)
        {
            Inquiry? inquiry = await this.context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                return this.NotFound();
            }

            inquiry.Response = replyDto.Response;
            inquiry.DateReplied = DateTime.Now;
            inquiry.Status = InquiryStatus.Replied;

            this.context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await this.context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.InquiryExists(id))
                {
                    return this.NotFound();
                }

                throw;
            }

            return this.NoContent();
        }

        // PUT: api/Inquiries/5/Close
        [HttpPut("{id}/Close")]
        public async Task<IActionResult> CloseInquiry(int id)
        {
            int? userId = ClaimsHelper.GetUserIdFromClaims(this.User);
            if (userId is null)
            {
                return this.Unauthorized();
            }

            Inquiry? inquiry = await this.context.Inquiries
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (inquiry == null)
            {
                return this.NotFound();
            }

            inquiry.Status = InquiryStatus.Closed;
            this.context.Entry(inquiry).State = EntityState.Modified;

            try
            {
                _ = await this.context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.InquiryExists(id))
                {
                    return this.NotFound();
                }

                throw;
            }

            return this.NoContent();
        }

        private bool InquiryExists(int id)
        {
            return this.context.Inquiries.Any(e => e.Id == id);
        }
    }
}