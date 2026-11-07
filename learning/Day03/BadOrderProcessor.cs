

public class BadOrderProcessor
{
    //vi phạm S: Process vừa validate, vừa tính giảm giá, vừa check payment, lưu file rồi thông báo, dòng 9 -28
    // public void Process(Order order, string customerType, string paymentMethod, string email)
    // {
    //     // 1. validate
    //     if (order.Items.Count == 0) throw new Exception("Empty order");

    //     // 2. discount
    //     decimal discount = 0;
    //     //Vi phạm O, nếu thêm 1 loại Customer Type thì lại else if thêm, dòng 14-15
    //     if (customerType == "VIP") discount = order.Total * 0.10m;
    //     else if (customerType == "Employee") discount = order.Total * 0.20m;
    //     var finalAmount = order.Total - discount;

    //     // 3. payment
    //     //Vi phạm O, if và else if rồi lại else, dòng 20 - 22
    //     if (paymentMethod == "Cash") Console.WriteLine($"Cash received: {finalAmount}");
    //     else if (paymentMethod == "Bank") Console.WriteLine($"Bank transfer requested: {finalAmount}");
    //     else throw new Exception("Unknown payment");

    //     // 4. save
    //     //vi phạm D: xử lý nghiệp vụ nhưng lại ghi trực tiếp file vào hàm xử lý nghiệp vụ, dòng 26
    //     File.AppendAllText("orders.log", $"{order.Id}:{finalAmount}\n");

    //     // 5. notify
    //     Console.WriteLine($"Email to {email}: order {order.Id}, total {finalAmount}");
    // }
}