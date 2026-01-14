using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    static class RetryHelper
    {
        // Executes an action with retry logic and structured logging
        public static void Execute(Action action, string operation, int maxRetries)
        {
            int attempt = 0;

            while (true)
            {
                try
                {
                    attempt++;
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        Log.Error(ex,
                            "Operation '{Operation}' failed after {Attempts} attempts",
                            operation, attempt);
                        return;
                    }

                    Log.Warning(ex,
                        "Operation '{Operation}' failed on attempt {Attempt}. Retrying...",
                        operation, attempt);
                }
            }
        }
    }
}
