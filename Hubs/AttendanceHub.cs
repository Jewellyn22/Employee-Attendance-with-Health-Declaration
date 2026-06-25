using Microsoft.AspNetCore.SignalR;

namespace ContractorAttendanceWithHealthDeclaration.Hubs
{
    public class AttendanceHub : Hub
    {
        private readonly ILogger<AttendanceHub> _logger;

        public AttendanceHub(ILogger<AttendanceHub> logger)
        {
            _logger = logger;
        }

        public async Task BroadcastAttendance(System.Collections.Generic.IEnumerable<object> todayAttendance)
        {
            await Clients.All.SendAsync("ReceiveAttendanceUpdate", todayAttendance);
            _logger.LogInformation("Broadcast attendance update to all clients");
        }

        public async Task BroadcastWaiverApproved(int attendance_id)
        {
            await Clients.All.SendAsync("WaiverApproved", attendance_id);
            _logger.LogInformation("Broadcast waiver approval for attendance: {AttendanceId}", attendance_id);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}