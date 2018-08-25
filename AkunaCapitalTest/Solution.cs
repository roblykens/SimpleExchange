/* Enter your code here. Read input using Console.ReadLine. Print output using Console.WriteLine. Your class should be named Solution */

/* --- TEST PROMISES THE FOLLOWING FUNCTIONAL RULES ---

1. All order id's will be UNIQUE

 */


/* -----  MY ASSUPTIONS  ------

1. As the user enters BUY and SELL orders, the app should keep all orders that are GFD orders in a list
2. As FULL orders are matched, they should be removed from the list
3. As PARCIAL orders are matched, the partical order should be reduced but stay in the list and it's counter part removed from the list
4. Because the GFD orders are 'GOOD FOR THE DAY", the app should timestamp them as if the orders don't get executed within 24 hours, remove them from the list of orders
5. Because a modify order must be removed from it's original order position for fullfillment, it makes sense to remove the order from the book and insert a "new" order
6. First column will only have ONE of FIVE values: BUY, SELL, CANCEL, MODIFY, PRINT
7. The exchange is an in memory app and does not require saving of the data if the app is closed

 */


// --- FORMAT ---

//IF in first column BUY or SELL then there are 5 columns for this line
//Second column will have IOC or GFD
//Third column is price (integer)
//Fourth column is Quantity (integer)
//Fifth column is Order Id - it can be arbitrary words

//If the first column is CANCEL then it has only 2 columns
//The second column is the Order Id

//--- MODIFY ---
//If the first column is MODIFY then it has five columns
//Second column will have the Order ID
//Third column is BUY or SELL
//Fourth column is price (integer)
//Fifth column is Quantity (integer)
//--- RULES ---
//If the Order ID doesn't exist - do nothing 
//If the modify order is an IOC order - it can't be modified
//A BUY can turn into a SELL and visa versa
//A MODIFY order LOOSES IT'S PLACE IN THE FULLFILLMENT ORDER AS IF IT WAS PLACED AS A NEW ORDER

//--- PRINT ---
//If the first column is PRINT then there are no other columns
//FUNCTION OF PRINT: To print out the current price book in the following format:
/*
 * SELL:
 * price1 qty1
 * price2 qty2
 * BUY:
 * price1 qty1
 * price2 qty2
 *
 * THE ITEMS MUST BE IN PRICE DECREASING ORDER 
 * IE:
 * SELL:
 * 1500 23
 * 1000 10
 * BUY:
 * 5000 5
 * 1000 500
 */

//INFORMATION:

//--- ORDER TYPES ---
//GFD (good for the day) stay in the order book until traded
//IOC (Insert or Cancel) order - If the order can't be traded immediately, it will be canceled. If it's partially traded, the non-traded part is cancelled.

//--- MATCHING RULE ---
//If there is a price that someone is willing to buy at an equal or higher from someone's selling price, these two order are traded. THE MATCHING ENGIN SHOULD ALSO PRINT OUT THE INFORMATION ABOUT THE TRADE WHEN IT OCCURES - AUTOMATICALLY!
//IE - imagin there are two order as noted below
//BUY GFD 1000 10 order1
//SELL GFD 1000 10 order20
//AFTER the second line has been processed then the output to the console should be 
//TRADE order1 1000 10 order20 1000 10
//
//--- RULES ----
//
//The matching should be FIFO - to be fair
//IE:
//BUY GFD 1000 10 order1
//BUY GFD 1000 10 order2
//SELL GFD 900 10 order3
//The sale should sell to the first buyer's request (order1) then to the next and so on (order2)
//OUTPUT:
//TRADE order1 1000 10 order3 900 10 
//TRADE order2 1000 10 order3 900 10 
//
//***IMPORTANT*** The FIFO rule is overridden if the price to buy is higher than another price to buy 
//IE
//BUY GFD 1000 10 ORDER1
//BUY GFD 1010 10 ORDER2
//SELL GFD 1000 15 ORDER3
//OUTPUT:
//TRADE ORDER2 1010 10 ORDER3 1000 10
//TRADE ORDER1 1000 5 ORDER3 1000 5
//This would leave ORDER1 with a quantity of 5 to still be left for a matching trade


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AkunaCapitalTest
{
    class Solution
    {

        private static List<SellOrder> _sellbook = new List<SellOrder>();
        private static List<BuyOrder> _buybook = new List<BuyOrder>();
        private static Timer _gfdTimer = new Timer(1000);

        static void Main(string[] args)
        {
            _gfdTimer.Elapsed += _gfdTimer_Elapsed;
            _gfdTimer.Enabled = true;
            _gfdTimer.Start();

            initForDebugging();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("The app has started. To end the program, type the word EXIT and press the enter key.");

            while (true) //keep accepting new commands until EXIT has been entered
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please enter your order or command or enter EXIT to end:");

                Console.ForegroundColor = ConsoleColor.Gray;
                var stdin = Console.ReadLine();

                if (stdin.ToUpper() == "EXIT")
                    Process.GetCurrentProcess().Kill();

                if (!checkInputFormat(stdin))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Your input was not in the correct format. Check the information and try again");
                    continue;
                }

                ConsoleColor cColor = ConsoleColor.DarkGray;
                var output = ParseAndExecuteCommand(stdin, out cColor);

                if (output == null)
                    continue;

                Console.ForegroundColor = cColor;
                Console.WriteLine(output);
            }
        }

        /// <summary>
        /// This timer will remove any orders that are over 1 day old
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _gfdTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string d1 = DateTime.UtcNow.ToString();
            string d2 = DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0)).ToString();
            _buybook.RemoveAll((b => b.timeStamp <= DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0))));
            _sellbook.RemoveAll((s => s.timeStamp <= DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0))));
        }

        /// <summary>
        /// The parse cammand will parse the command from the string entered and execute the command
        /// </summary>
        /// <param name="stdin"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private static string ParseAndExecuteCommand(string stdin, out ConsoleColor color)
        {
            color = ConsoleColor.Gray;
            SellOrder _sOrder = null;
            BuyOrder _bOrder = null;
            string _return = null;


            var lineArr = stdin.Split(' ');
            switch (lineArr[0].ToUpper())
            {
                case "BUY":
                    //create order
                    _bOrder = new BuyOrder()
                    {
                        OrderID = lineArr[4],
                        Price = Int32.Parse(lineArr[2]),
                        Qty = Int32.Parse(lineArr[3]),
                        timeStamp = DateTime.UtcNow
                    };

                    //check order type
                    if (lineArr[1].ToUpper() == "IOC")
                    {
                        //go directly to match - don't hold the order
                        color = ConsoleColor.DarkCyan;
                        return MatchOrders(_bOrder);
                    }

                    //insert into buy list
                    _buybook.Add(_bOrder);

                    //do match
                    color = ConsoleColor.Cyan;
                    _return = MatchOrders(_bOrder);
                    break;

                case "SELL":

                    _sOrder = new SellOrder()
                    {
                        OrderID = lineArr[4],
                        Price = Int32.Parse(lineArr[2]),
                        Qty = Int32.Parse(lineArr[3]),
                        timeStamp = DateTime.UtcNow
                    };

                    //check order type
                    if (lineArr[1].ToUpper() == "IOC")
                    {
                        //go directly to match - don't hold the order
                        color = ConsoleColor.DarkCyan;
                        return MatchOrders(null, _sOrder);
                    }


                    //insert into sell list
                    _sellbook.Add(_sOrder);

                    //do match
                    color = ConsoleColor.Cyan;
                    _return = MatchOrders(null, _sOrder);
                    break;

                case "MODIFY":
                    //find order and remove
                    try
                    {
                        //remove it from buy or sell list
                        var foundOrder = RemoveOrder(lineArr[1]);

                        //insert new order into buy or sell list
                        if (foundOrder)
                        {
                            //add the "new" order:
                            if (lineArr[2] == "BUY")
                            {
                                _bOrder = new BuyOrder()
                                {
                                    OrderID = lineArr[1],
                                    Price = Int32.Parse(lineArr[3]),
                                    Qty = Int32.Parse(lineArr[4]),
                                    timeStamp = DateTime.UtcNow
                                };
                                _buybook.Add(_bOrder);
                                //do match
                                color = ConsoleColor.DarkCyan;
                                _return = MatchOrders(_bOrder);
                            }
                            else
                            {
                                _sOrder = new SellOrder()
                                {
                                    OrderID = lineArr[1],
                                    Price = Int32.Parse(lineArr[3]),
                                    Qty = Int32.Parse(lineArr[4]),
                                    timeStamp = DateTime.UtcNow
                                };
                                _sellbook.Add(_sOrder);
                                //do match
                                color = ConsoleColor.DarkCyan;
                                _return = MatchOrders(null, _sOrder);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        color = ConsoleColor.Red;
                        return "ERROR: An internal error has occured.\r\n" + "TECHNICAL ERROR DETAILS: " + e.Message;
                    }
                    break;

                case "CANCEL":
                    //remove the order from buy or sell list
                    RemoveOrder(lineArr[1]);
                    _return = null;
                    break;

                case "PRINT":
                    //loop through lists and present as required from books (buy and sell list)
                    color = ConsoleColor.Gray;
                    _return = Print();

                    break;

                default:
                    //the command is wrong!
                    color = ConsoleColor.Red;
                    _return = "ERROR! The order or command type is not recognized! Acceptable commands are: BUY, SELL, MODIFY, CANCEL, or PRINT";
                    break;
            }

            return _return;
        }

        private static string MatchOrders(BuyOrder buyOrder = null, SellOrder sellOrder = null)
        {
            /******* format: TRADE | ORDERID OF BOOK ORDER | BOOK ORDER PRICE | QUANTITY TAKEN FROM BOOK ORDER | ORDER ID | ORDER PRICE | QUANTIY RECEIVED ****/
            var _output = new StringBuilder();
            if (buyOrder != null)
            {

                //select sells with a price less than or equal to the buy price and sort sells by price asending and then by datetime 
                var sellOrders = _sellbook.FindAll(s => s.Price <= buyOrder.Price).OrderBy(s => s.Price)
                    .ThenBy(s => s.timeStamp);

                foreach (var order in sellOrders)
                {
                    //save each order that was affected and print the output
                    _output.Append("TRADE ");
                    _output.Append(order.OrderID);
                    _output.Append(" ");
                    _output.Append(order.Price);
                    _output.Append(" ");

                    //if partial
                    if (buyOrder.Qty >= order.Qty)
                    {
                        //the quantity is equal to the order.qty for the order in the book order
                        _output.Append(order.Qty);
                        _output.Append(" ");
                        _output.Append(buyOrder.OrderID);
                        _output.Append(" ");
                        _output.Append(buyOrder.Price);
                        _output.Append(" ");
                        _output.Append(order.Qty);
                        _output.Append("\r\n");

                        //reduce the order's quantity
                        buyOrder.Qty -= order.Qty;

                        //remove sell for any sell where it's quantity can be depleted to 0
                        _sellbook.Remove(order);

                        //remove the buy order if it was the same quantity otherwise change the quantiy available for the sell where a parcial quantiy was taken
                        if (buyOrder.Qty == 0)
                        {
                            _buybook.Remove(buyOrder);
                            break;
                        }

                        _buybook.Where(b => b.OrderID == buyOrder.OrderID).ToList().First().Qty = buyOrder.Qty;
                    }
                    //if full
                    else
                    {
                        //the QTY amount in the book order has is more than the order QTY
                        _sellbook.Where(b => b.OrderID == order.OrderID).ToList().First().Qty = order.Qty - buyOrder.Qty;

                        //remove the buy order
                        _buybook.Remove(buyOrder);

                        //the quantity is equal to the buyOrder.qty for the order and the book order
                        _output.Append(buyOrder.Qty);
                        _output.Append(" ");
                        _output.Append(buyOrder.OrderID);
                        _output.Append(" ");
                        _output.Append(buyOrder.Price);
                        _output.Append(" ");
                        _output.Append(buyOrder.Qty);
                        _output.Append("\r\n");
                        break;
                    }
                }
            }
            else
            {
                //select buys with a price greater than or equal to the sell price and sort buys by price desending and then by datetime 
                var buyOrders = _buybook.FindAll(s => s.Price >= sellOrder.Price).OrderByDescending(s => s.Price)
                    .ThenBy(s => s.timeStamp);

                foreach (var order in buyOrders)
                {

                    //save each order that was affected and print the output
                    _output.Append("TRADE ");
                    _output.Append(order.OrderID);
                    _output.Append(" ");
                    _output.Append(order.Price);
                    _output.Append(" ");


                    if (sellOrder.Qty >= order.Qty)
                    {
                        //the quantity is equal to the order.qty for the order in the book order
                        _output.Append(order.Qty);
                        _output.Append(" ");
                        _output.Append(sellOrder.OrderID);
                        _output.Append(" ");
                        _output.Append(sellOrder.Price);
                        _output.Append(" ");
                        _output.Append(order.Qty);
                        _output.Append("\r\n");

                        //reduce the order's quantity
                        sellOrder.Qty -= order.Qty;

                        //remove sell for any sell where it's quantity can be depleted to 0
                        _buybook.Remove(order);

                        //remove the buy order if it was the same quantity otherwise change the quantiy available for the sell where a parcial quantiy was taken
                        if (sellOrder.Qty == 0)
                        {
                            _sellbook.Remove(sellOrder);
                            break;
                        }

                        _sellbook.Where(b => b.OrderID == sellOrder.OrderID).ToList().First().Qty = sellOrder.Qty;
                    }
                    else
                    {
                        //the QTY amount in the book order has is more than the order QTY
                        _buybook.Where(b => b.OrderID == order.OrderID).ToList().First().Qty = order.Qty - sellOrder.Qty;

                        //remove the buy order
                        _sellbook.Remove(sellOrder);

                        //the quantity is equal to the sellOrder.qty for the order and the book order
                        _output.Append(sellOrder.Qty);
                        _output.Append(" ");
                        _output.Append(sellOrder.OrderID);
                        _output.Append(" ");
                        _output.Append(sellOrder.Price);
                        _output.Append(" ");
                        _output.Append(sellOrder.Qty);
                        _output.Append("\r\n");
                        break;
                    }
                }
            }

            return _output.ToString();
        }
        /// <summary>
        /// This will remove the order if found in either book list
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        private static bool RemoveOrder(string orderID)
        {
            //remove it from buy or sell list
            var foundBuyOrderOrder = _buybook.Remove(_buybook.Find(b => b.OrderID == orderID));
            var foundsSellOrder = _sellbook.Remove(_sellbook.Find(s => s.OrderID == orderID));

            //insert new order into buy or sell list
            if (foundBuyOrderOrder || foundsSellOrder)
                return true;

            return false;
        }

        private static string Print()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SELL:");
            var sells = _sellbook
                .GroupBy(buy => new { buy.Price })
                .Select(g => new PrintOutput() { Price = g.Key.Price, Qty = g.Sum(r => r.Qty) });

            foreach (var sell in sells)
            {
                builder.Append(sell.Price.ToString());
                builder.Append(" ");
                builder.Append(sell.Qty.ToString());
                builder.Append("\r\n");
            }

            builder.AppendLine("BUY:");
            var buys = _buybook
                .GroupBy(buy => new { buy.Price })
                .Select(g => new PrintOutput() { Price = g.Key.Price, Qty = g.Sum(r => r.Qty) });

            foreach (var buy in buys)
            {
                builder.Append(buy.Price.ToString());
                builder.Append(" ");
                builder.Append(buy.Qty.ToString());
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        private static bool checkInputFormat(string args)
        {
            if (!string.IsNullOrEmpty(args))
            {
                var strArr = args.Split(' ');

                switch (strArr[0].ToUpper())
                {
                    case "BUY":
                    case "SELL":

                        if (strArr.Length != 5)
                            return false;

                        if (!strArr[1].ToUpper().Contains("GFD") && !strArr[1].ToUpper().Contains("IOC"))
                        {
                            return false;
                        }

                        int t;
                        if (!int.TryParse(strArr[2], out t) || !int.TryParse(strArr[3], out t))
                        {
                            return false;
                        }

                        if (string.IsNullOrEmpty(strArr[4]))
                            return false;
                        break;

                    case "MODIFY":
                        if (strArr.Length != 5)
                            return false;

                        if (string.IsNullOrEmpty(strArr[1]))
                            return false;

                        if (!strArr[2].ToUpper().Contains("BUY") && !strArr[2].ToUpper().Contains("SELL"))
                        {
                            return false;
                        }

                        int m;
                        if (!int.TryParse(strArr[3], out m) || !int.TryParse(strArr[4], out m))
                        {
                            return false;
                        }

                        break;
                    case "PRINT":
                        if (strArr.Length != 1)
                            return false;
                        break;

                    case "CANCEL":
                        if (strArr.Length != 2)
                            return false;
                        if (string.IsNullOrEmpty(strArr[1]))
                            return false;
                        break;
                    default:
                        return false;
                }

            }

            return true;
        }

        private static void initForDebugging()
        {
            _buybook.Add(new BuyOrder() { OrderID = "O1", Price = 1000, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O2", Price = 1000, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O3", Price = 500, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O4", Price = 1020, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O5", Price = 500, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O6", Price = 100, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O7", Price = 230, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O8", Price = 500, Qty = 10, timeStamp = DateTime.UtcNow });
            _buybook.Add(new BuyOrder() { OrderID = "O9", Price = 1000, Qty = 10, timeStamp = DateTime.UtcNow });

            _sellbook.Add(new SellOrder() { OrderID = "O100", Price = 2000, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O200", Price = 2000, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O300", Price = 1500, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O400", Price = 1020, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O500", Price = 1500, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O600", Price = 100, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O700", Price = 230, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O800", Price = 1500, Qty = 10, timeStamp = DateTime.UtcNow });
            _sellbook.Add(new SellOrder() { OrderID = "O900", Price = 2000, Qty = 10, timeStamp = DateTime.UtcNow });

        }
    }
}
public class Order
{
    public int Price { get; set; }
    public int Qty { get; set; }
    public string OrderID { get; set; }
    public DateTime timeStamp { get; set; }
}

public class SellOrder : Order
{
    public readonly string OType = "SELL";
}

public class BuyOrder : Order
{
    public readonly string OType = "BUY";
}

public class PrintOutput
{
    public int Price { get; set; }
    public int Qty { get; set; }
}
