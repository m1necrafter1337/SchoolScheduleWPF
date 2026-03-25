using System;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolSchedule.Services
{
    public class AppDataChangedNotifier
    {
        public event Func<Task>? DataChanged;

        public async Task NotifyDataChangedAsync()
        {
            if (DataChanged == null) return;
            var handlers = DataChanged.GetInvocationList().Cast<Func<Task>>();
            foreach (var handler in handlers)
                await handler();
        }
    }
}
