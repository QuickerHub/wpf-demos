using System;
using System.Threading.Tasks;

namespace QuickerStatisticsInfo.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Quicker Statistics Info - Console Test");
            System.Console.WriteLine("=====================================");
            System.Console.WriteLine();

            var collector = new QuickerStatisticsCollector();
            
            try
            {
                // Test with the specified user path
                string userPath = "113342-Cea";
                
                System.Console.WriteLine($"Collecting statistics for user: {userPath}");
                System.Console.WriteLine("Please wait...");
                System.Console.WriteLine();

                // Get detailed statistics
                var result = await collector.GetStatisticsAsync(userPath);

                System.Console.WriteLine($"Statistics for user: {result.UserPath}");
                System.Console.WriteLine($"Total pages: {result.PageStatistics.Count}");
                System.Console.WriteLine();

                // Display per-page statistics
                foreach (var pageStat in result.PageStatistics)
                {
                    System.Console.WriteLine($"Page {pageStat.PageNumber}: {pageStat.LikesCount} likes");
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Total Likes: {result.TotalLikes}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                collector.Dispose();
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }
    }
}

