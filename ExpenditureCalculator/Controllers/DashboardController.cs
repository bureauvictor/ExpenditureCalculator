using ExpenditureCalculator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ExpenditureCalculator.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index()
        {
            //Last 7 days
            DateTime startTime= DateTime.Now.AddDays(-3500); //modify at ease
            DateTime endTime= DateTime.Now;

            List<Transaction> selectedTransaction = await _context.Transactions
                .Include(x=> x.Category)
                .Where(y=> y.Date >= startTime && y.Date <= endTime)
                .ToListAsync();
            ViewBag.AllTransactions = selectedTransaction;


            //Total Income
            int totalIncome = selectedTransaction
                .Where(x => x.Category.Type == "Income")
                .Sum(x=> x.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0");

            //Total Expense
            int totalExpense = selectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .Sum(x => x.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0");

            //Net Balance
            int netBalance = totalIncome - totalExpense;
            ViewBag.NetBalance = netBalance.ToString("C0");

            //Data For Doughnut - Expense By Category

            var incomeInformation = selectedTransaction
                .Where(x => x.Category.Type == "Income")
                .GroupBy(x => x.CategoryId)
                .Select(group => new
                {
                    TotalSum = group.Sum(x=> x.Amount),
                    Title= group.Select(x=> x.CategoryTitleWithIcon).FirstOrDefault(),
                    FormattedAmount = group.Sum(x => x.Amount).ToString("C0")

                })
                .OrderByDescending(x => x.TotalSum)
                .ToList();

            ViewBag.IncomeChart = incomeInformation;

            //Data For Doughnut - Income By Category


            var expenseInformation = selectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(x => x.CategoryId)
                .Select(group => new
                {
                    TotalSum = group.Sum(x => x.Amount),
                    Title = group.Select(x => x.CategoryTitleWithIcon).FirstOrDefault(),
                    FormattedAmount = group.Sum(x => x.Amount).ToString("C0")
                })
                .OrderByDescending(x=> x.TotalSum)
                .ToList();

            ViewBag.ExpenseChart = expenseInformation;


            //Data for Spline-chart
            List<SplineChartData> incomeSummary = selectedTransaction
                .Where(x => x.Category.Type == "Income")
                .GroupBy(x => x.Date)
                .Select(group => new SplineChartData()
                {
                    Days= group.First().Date.ToString("dd-MM-yyyy"),
                    DailyIncomeAmount = group.Sum(x=> x.Amount)
                })
                .ToList();

            List<SplineChartData> expenseSummary = selectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(x => x.Date)
                .Select(group => new SplineChartData()
                {
                    Days = group.First().Date.ToString("dd-MM-yyyy"),
                    DailyExpenditureAmount = group.Sum(x => x.Amount)
                })
                .ToList();

            List<SplineChartData> balanceSummary = selectedTransaction.GroupBy(x => x.Date)
                .Select(x => new SplineChartData()
                {
                    Days = x.First().Date.ToString("dd-MM-yyyy"),
                    DailyBalance = x.Where(y=> y.Category.Type == "Income").Sum(y=> y.Amount) 
                                    - x.Where(y => y.Category.Type == "Expense").Sum(y => y.Amount)

                })
                .OrderBy(x => x.Days)
                .ToList();


            string[] last7days;


            if (selectedTransaction.Exists(x=> x.Date==DateTime.Today))
            {
                DateTime firstDate = selectedTransaction.OrderBy(x => x.Date).Select(x => x.Date).First();
                DateTime lastDate = selectedTransaction.OrderBy(x => x.Date).Select(x => x.Date).Last();

                // Calculate the difference in days
                TimeSpan difference = lastDate.Subtract(firstDate);
                int differenceInDays = (int)difference.TotalDays;

                last7days = Enumerable.Range(0, differenceInDays)
                .Select(i => firstDate.AddDays(i).ToString("dd-MM-yyyy"))
                .ToArray();
            }
            else
            {
                last7days = Enumerable.Range(0, 7) // These are the dates where the transactions are located february7days
                .Select(i => DateTime.Parse("23-02-2024").AddDays(i).ToString("dd-MM-yyyy"))
                .ToArray();
            }

            int previousbalance = 0;
            foreach (var day in last7days)
            {
                if (balanceSummary.Any( x => x.Days == day))
                {
                    previousbalance += balanceSummary.Where(x => x.Days == day).Select(x => x.DailyBalance).First();
                    foreach (var day2 in balanceSummary.Where(x => x.Days == day))
                    {
                        day2.DailyBalance = previousbalance;
                    }
                }
                else
                {
                    //previousbalance += balanceSummary.Where(x => x.Days == day).Select(x => x.DailyBalance).First();
                    balanceSummary.Add(new SplineChartData() { Days = day, DailyBalance = previousbalance });
                    
                }
            }

            ViewBag.SplineData = from day in last7days
                                 join income in incomeSummary on day equals income.Days into dayIncomeJoined
                                 from income in dayIncomeJoined.DefaultIfEmpty()
                                 join expense in expenseSummary on day equals expense.Days into expenseJoined
                                 from expense in expenseJoined.DefaultIfEmpty()
                                 join balance in balanceSummary on day equals balance.Days into balanceJoined
                                 from balance in balanceJoined.DefaultIfEmpty()
                                 select new
                                 {
                                     day=day,
                                     income = income == null? 0: income.DailyIncomeAmount,
                                     expense = expense == null? 0: expense.DailyExpenditureAmount,
                                     dailyBalance = balance == null ? 0 : balance.DailyBalance
                                 };

            //Recent Transactions

            List<Transaction> recentTransactions = _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(x=> x.Date)
                .Take(10)
                .ToList();

            ViewBag.RecentTransactions= recentTransactions;

            return View();
        }
        public class SplineChartData
        {
            public string? Days;
            public int DailyIncomeAmount;
            public int DailyExpenditureAmount;
            public int DailyBalance;

        }

    }
}
