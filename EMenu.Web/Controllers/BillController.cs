using Microsoft.AspNetCore.Mvc;
using EMenu.Infrastructure.Data;

[Route("api/[controller]")]
[ApiController]
public class BillController : Controller
{
    public IActionResult Index(int tableId)
    {
        ViewBag.TableId = tableId;
        return View();
    }

    private readonly AppDbContext _context;

    public BillController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("table")]
    public IActionResult GetBill(int tableId)
    {
        var session = _context.OrderSessions
            .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);

        if (session == null)
            return NotFound("Session not found");

        var items = _context.OrderProducts
            .Join(_context.Orders,
                op => op.OrderID,
                o => o.OrderID,
                (op, o) => new { op, o })
            .Join(_context.Products,
                x => x.op.ProductID,
                p => p.ProductID,
                (x, p) => new
                {
                    p.ProductName,
                    x.op.Quantity,
                    p.Price,
                    Total = x.op.Quantity * p.Price
                })
            .ToList();

        var total = items.Sum(x => x.Total);

        return Ok(new
        {
            Items = items,
            Total = total
        });
    }

    [HttpPost("checkout")]
    public IActionResult Checkout(int tableId)
    {
        var session = _context.OrderSessions
            .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);

        if (session == null)
            return NotFound("Session not found");

        session.Status = 0;
        session.EndTime = DateTime.Now;

        var table = _context.RestaurantTables
            .FirstOrDefault(x => x.TableID == tableId);

        table.Status = 0;

        _context.SaveChanges();

        return Ok();
    }

}