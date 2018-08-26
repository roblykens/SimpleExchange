/* Enter your code here. Read input using Console.ReadLine. Print output using Console.WriteLine. Your class should be named Solution */

/* --- TEST PROMISES THE FOLLOWING FUNCTIONAL RULES ---

    1. All order id's will be UNIQUE

 */


/* -----  MY ASSUPTIONS  ------

    1. As the user enters BUY and SELL orders, the app should keep all orders that are GFD orders in a list
    2. As FULL orders are matched, they should be removed from the list
    3. As PARCIAL orders are matched, the partical order should be reduced but stay in the list and it's counter part removed from the list
    4. Because the GFD orders are 'GOOD FOR THE DAY", the app should timestamp them as if the orders don't get executed within 24 hours, remove them from the list of orders EVERY MINUTE
    5. Because a modify order must be removed from it's original order position for fullfillment, it makes sense to remove the order from the book and insert a "new" order
    6. First column will only have ONE of FIVE values: BUY, SELL, CANCEL, MODIFY, PRINT
    7. The exchange is an in memory app and does not require saving of the data if the app is closed
    8. The app should work as instructed in the specification

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace AkunaCapitalTest
{
    class Solution
    {

        private static List<SellOrder> _sellbook = new List<SellOrder>();
        private static List<BuyOrder> _buybook = new List<BuyOrder>();
        private static Timer _gfdTimer = new Timer(60000);

        static void Main(string[] args)
        {
            //set timer to purge GFD orders over 24 hours old
            _gfdTimer.Elapsed += _gfdTimer_Elapsed;
            _gfdTimer.Enabled = true;
            _gfdTimer.Start();

            string stdin;
            while ((stdin = Console.ReadLine()) != null) //keep accepting new commands until null line
            {
                //if false - ingore the line
                if (checkInputFormat(stdin))
                {
                    var output = ParseAndExecuteCommand(stdin);
                    if (!string.IsNullOrEmpty(output))
                        Console.Write(output);
                }
            }
        }

        /// <summary>
        /// This timer will remove any orders that are over 1 day old
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _gfdTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _buybook.RemoveAll((b => b.timeStamp <= DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0))));
            _sellbook.RemoveAll((s => s.timeStamp <= DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0))));
        }

        /// <summary>
        /// The parse cammand will parse the command from the string entered and execute the command
        /// </summary>
        /// <param name="stdin"></param>
        /// <returns></returns>
        private static string ParseAndExecuteCommand(string stdin)
        {
            if (stdin == null)
                return null;

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
                        OType = lineArr[1],
                        OrderID = lineArr[4],
                        Price = Int32.Parse(lineArr[2]),
                        Qty = Int32.Parse(lineArr[3]),
                        timeStamp = DateTime.UtcNow
                    };

                    //check order type
                    if (lineArr[1].ToUpper() == "IOC")
                    {
                        //go directly to match - don't hold the order
                        _return = MatchOrders(_bOrder);
                    }
                    else
                    {
                        //insert into buy list
                        _buybook.Add(_bOrder);

                        //do match
                        _return = MatchOrders(_bOrder);
                    }
                    break;

                case "SELL":

                    _sOrder = new SellOrder()
                    {
                        OType = lineArr[1],
                        OrderID = lineArr[4],
                        Price = Int32.Parse(lineArr[2]),
                        Qty = Int32.Parse(lineArr[3]),
                        timeStamp = DateTime.UtcNow
                    };

                    //check order type
                    if (lineArr[1].ToUpper() == "IOC")
                    {
                        //go directly to match - don't hold the order
                        return MatchOrders(null, _sOrder);
                    }
                    else
                    {
                        //insert into sell list
                        _sellbook.Add(_sOrder);
                        //do match

                        _return = MatchOrders(null, _sOrder);
                    }

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
                                    OType = "GFD",
                                    OrderID = lineArr[1],
                                    Price = Int32.Parse(lineArr[3]),
                                    Qty = Int32.Parse(lineArr[4]),
                                    timeStamp = DateTime.UtcNow
                                };
                                _buybook.Add(_bOrder);

                                //do match
                                _return = MatchOrders(_bOrder);
                            }
                            else
                            {
                                _sOrder = new SellOrder()
                                {
                                    OType = "GFD",
                                    OrderID = lineArr[1],
                                    Price = Int32.Parse(lineArr[3]),
                                    Qty = Int32.Parse(lineArr[4]),
                                    timeStamp = DateTime.UtcNow
                                };
                                _sellbook.Add(_sOrder);

                                //do match
                                _return = MatchOrders(null, _sOrder);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        return "ERROR: An internal error has occured.\r\n" + "TECHNICAL ERROR DETAILS: " + e.Message;
                    }
                    break;

                case "CANCEL":
                    //remove the order from buy or sell list
                    RemoveOrder(lineArr[1]);
                    break;

                case "PRINT":
                    //loop through lists and present as required from books (buy and sell list)
                    _return = Print();
                    break;

                default:
                    //the command is wrong!
                    _return = "ERROR! The order or command type is not recognized! Acceptable commands are: BUY, SELL, MODIFY, CANCEL, or PRINT";
                    break;
            }

            return _return;
        }

        /// <summary>
        /// This method will receive a buy or a sell order and find matches.
        /// The two paramitors are optional with a check to make sure at
        /// least one is not null before processing.
        /// </summary>
        /// <param name="buyOrder"></param>
        /// <param name="sellOrder"></param>
        /// <returns></returns>
        private static string MatchOrders(BuyOrder buyOrder = null, SellOrder sellOrder = null)
        {
            if (buyOrder == null && sellOrder == null)
            {
                return null;
            }

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
                        try
                        {
                            if (buyOrder.OType.ToUpper() == "GFD")
                                _buybook.Where(b => b.OrderID == buyOrder.OrderID).ToList().First().Qty = buyOrder.Qty;
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Sellbookk quantity is {sellOrder.Qty}");
                        }
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

                        try
                        {
                            if (sellOrder.OType.ToUpper() == "GFD")
                                _sellbook.Where(b => b.OrderID == sellOrder.OrderID).ToList().First().Qty = sellOrder.Qty;
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Sellbookk quantity is {sellOrder.Qty}");
                        }

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

        /// <summary>
        /// This method will return all current BUY and SELL prices and their quantities
        /// </summary>
        /// <returns></returns>
        private static string Print()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SELL:");
            var sells = _sellbook
                .GroupBy(buy => new { buy.Price })
                .Select(g => new PrintOutput() { Price = g.Key.Price, Qty = g.Sum(r => r.Qty) }).OrderByDescending(v => v.Price);

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
                .Select(g => new PrintOutput() { Price = g.Key.Price, Qty = g.Sum(r => r.Qty) }).OrderByDescending(v => v.Price);

            foreach (var buy in buys)
            {
                builder.Append(buy.Price.ToString());
                builder.Append(" ");
                builder.Append(buy.Qty.ToString());
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        /// <summary>
        /// This method will check the input string from the read console line
        /// to ensure that the line matches formating and if there is an error
        /// will send back a false which will make the proces ingore the input.
        /// ***NOTE*** Normally, you would send back an error message with the
        /// formatting issues found.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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

                        int t1, t2;
                        if (!int.TryParse(strArr[2], out t1) || !int.TryParse(strArr[3], out t2))
                        {
                            return false;
                        }

                        if (t1 <= 0 || t2 <= 0)
                            return false;

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

                        int m1, m2;
                        if (!int.TryParse(strArr[3], out m1) || !int.TryParse(strArr[4], out m2))
                        {
                            return false;
                        }

                        if (m1 <= 0 || m2 <= 0)
                            return false;

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
            else
            {
                return false;
            }

            return true;
        }
    }
}
/*
 * The following are data objects to hold order data or return it.
 * The use of a base class order then a derived class of each kind
 * of order was used. 
 */

public class Order
{
    public int Price { get; set; }
    public int Qty { get; set; }
    public string OrderID { get; set; }
    public DateTime timeStamp { get; set; }
}

public class SellOrder : Order
{
    public string OType { get; set; }
}

public class BuyOrder : Order
{
    public string OType { get; set; }
}

public class PrintOutput
{
    public int Price { get; set; }
    public int Qty { get; set; }
}
