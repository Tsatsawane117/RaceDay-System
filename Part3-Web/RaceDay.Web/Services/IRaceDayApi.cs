using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public interface IRaceDayApi
{
    // Authentication
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password);
    Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<MessageResponse>> LogoutAsync();

    // Profile
    Task<ApiResult<UserDto>> GetProfileAsync();
    Task<ApiResult<UserDto>> UpdateProfileAsync(string fullName, string? contactNumber);
    Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync(string? eventType = null);
    Task<ApiResult<EventDto>> GetEventAsync(int id);
    Task<ApiResult<List<EventSummaryDto>>> GetMyEventsAsync();
    Task<ApiResult<EventDto>> CreateEventAsync(EventRequest request);
    Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult<MessageResponse>> DeleteEventAsync(int id);
    Task<ApiResult<EventDto>> UploadBannerAsync(int eventId, IFormFile file);

    // Categories
    Task<ApiResult<CategoryDto>> AddCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult<MessageResponse>> DeleteCategoryAsync(int categoryId);

    // Enrolments and payments
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<MessageResponse>> CancelEnrolmentAsync(int enrolmentId);
    Task<ApiResult<PaymentDto>> PayAsync(int enrolmentId, decimal amount, string paymentMethod);

    // Results
    Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId);
    Task<ApiResult<ResultDto>> CaptureResultAsync(ResultRequest request);
    Task<ApiResult<List<MyResultDto>>> GetMyResultsAsync();
}
