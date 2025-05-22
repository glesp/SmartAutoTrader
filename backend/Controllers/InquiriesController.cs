/* <copyright file="InquiriesController.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the InquiriesController class, which provides API endpoints for managing user inquiries in the Smart Auto Trader application.
</summary>
<remarks>
The InquiriesController class allows users to create, retrieve, and manage inquiries about vehicles. It also includes administrative endpoints for managing inquiries, such as marking them as read, replying, or closing them. The controller uses dependency injection for the ApplicationDbContext to interact with the database and is secured with the [Authorize] attribute to restrict access to authenticated users. Some endpoints are further restricted to users with the "Admin" role.
</remarks>
<dependencies>
- Microsoft.AspNetCore.Authorization
- Microsoft.AspNetCore.Mvc
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Data
- SmartAutoTrader.API.DTOs
- SmartAutoTrader.API.Enums
- SmartAutoTrader.API.Helpers
- SmartAutoTrader.API.Models
</dependencies>
 */

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

        /// <summary>
        /// Retrieves a specific inquiry by its ID for the authenticated user.
        /// </summary>
        /// <param name="id">The ID of the inquiry to retrieve.</param>
        /// <returns>The inquiry with the specified ID, if it exists and belongs to the authenticated user.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the inquiry does not exist or does not belong to the user.</exception>
        /// <remarks>
        /// This method retrieves a specific inquiry for the authenticated user, including related vehicle information.
        /// </remarks>
        /// <example>
        /// GET /api/Inquiries/5.
        /// </example>
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

        /// <summary>
        /// Retrieves all inquiries, optionally filtered by status, for administrative purposes.
        /// </summary>
        /// <param name="status">The status to filter inquiries by. Optional.</param>
        /// <returns>A list of inquiries, optionally filtered by status.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated or lacks the "Admin" role.</exception>
        /// <remarks>
        /// This method is restricted to users with the "Admin" role and allows filtering inquiries by their status.
        /// </remarks>
        /// <example>
        /// GET /api/Inquiries/admin?status=New.
        /// </example>
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

        /// <summary>
        /// Creates a new inquiry for the authenticated user.
        /// </summary>
        /// <param name="inquiryDto">The DTO containing the details of the inquiry to create.</param>
        /// <returns>The created inquiry.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the vehicle specified in the inquiry does not exist.</exception>
        /// <remarks>
        /// This method creates a new inquiry for the authenticated user and associates it with the specified vehicle.
        /// </remarks>
        /// <example>
        /// POST /api/Inquiries
        /// Body: { "vehicleId": 123, "subject": "Inquiry about the car", "message": "Is this car still available?" }.
        /// </example>
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

        /// <summary>
        /// Marks a specific inquiry as read. Restricted to administrators.
        /// </summary>
        /// <param name="id">The ID of the inquiry to mark as read.</param>
        /// <returns>No content if the operation is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the inquiry does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated or lacks the "Admin" role.</exception>
        /// <remarks>
        /// This method updates the status of the specified inquiry to "Read".
        /// </remarks>
        /// <example>
        /// PUT /api/Inquiries/5/MarkAsRead.
        /// </example>
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

        /// <summary>
        /// Replies to a specific inquiry. Restricted to administrators.
        /// </summary>
        /// <param name="id">The ID of the inquiry to reply to.</param>
        /// <param name="replyDto">The DTO containing the reply message.</param>
        /// <returns>No content if the operation is successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the inquiry does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated or lacks the "Admin" role.</exception>
        /// <remarks>
        /// This method updates the inquiry with the reply message and changes its status to "Replied".
        /// </remarks>
        /// <example>
        /// PUT /api/Inquiries/5/Reply
        /// Body: { "response": "Thank you for your inquiry. The car is still available." }.
        /// </example>
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

        /// <summary>
        /// Closes a specific inquiry for the authenticated user.
        /// </summary>
        /// <param name="id">The ID of the inquiry to close.</param>
        /// <returns>No content if the operation is successful.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the inquiry does not exist or does not belong to the user.</exception>
        /// <remarks>
        /// This method updates the status of the specified inquiry to "Closed".
        /// </remarks>
        /// <example>
        /// PUT /api/Inquiries/5/Close.
        /// </example>
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

        /// <summary>
        /// Checks if an inquiry with the specified ID exists in the database.
        /// </summary>
        /// <param name="id">The ID of the inquiry to check.</param>
        /// <returns>True if the inquiry exists; otherwise, false.</returns>
        private bool InquiryExists(int id)
        {
            return this.context.Inquiries.Any(e => e.Id == id);
        }
    }
}