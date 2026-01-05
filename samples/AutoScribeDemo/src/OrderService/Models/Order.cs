namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string State { get; set; } = "CA";
    public string Address { get; set; } = "123 Main St";
    public string PaymentMethod { get; set; } = "card_123";

    public override string ToString()
    {
        return $"new User {{ Id = {Id}, Name = \"{Name}\", Email = \"{Email}\" }}";
    }
}
