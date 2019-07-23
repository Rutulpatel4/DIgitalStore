using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalStore.Models;

namespace DigitalStore.Controllers
{
    public class StoreController : Controller
    {
        private DigitalModel db = new DigitalModel();
        // GET: Store
        public ActionResult Index(string productBrand2, string searchString)
        {

              var categories = db.Categories.OrderBy(g => g.Name).ToList();
            var BrandList = new List<string>();

            var BrandQry = from d in db.Products
                           orderby d.Brand.Name
                           select d.Brand.Name;

            BrandList.AddRange(BrandQry.Distinct());
            ViewBag.productBrand2 = new SelectList(BrandList);

            var products = from p in db.Products
                           select p;


            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Title.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(productBrand2))
            {
                products = products.Where(x => x.Brand.Name == productBrand2);
            }
            return View(categories);
        }

        public ActionResult Search(string productBrand1, string searchString)
        {
            var BrandList = new List<string>();

            var BrandQry = from d in db.Products
                           orderby d.Brand.Name
                           select d.Brand.Name;

            BrandList.AddRange(BrandQry.Distinct());
            ViewBag.productBrand1 = new SelectList(BrandList);
            //ViewBag.productBrand = new SelectList(BrandList);

            var products = from p in db.Products
                           select p;


            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Title.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(productBrand1))
            {
                products = products.Where(x => x.Brand.Name == productBrand1);
            }

            return View(products);

        }

      //  GET STORE/Products? Category
        public ActionResult Products(string category)
        {
            var products = db.Products.Where(p => p.Category.Name == category).ToList();
            return View(products);
        }
        //GET: STORE/PRODUCT
        public ActionResult Pro(String ProName)
        {
            ViewBag.ProName = ProName;
            return View();
        }

        //GET: STORE/AddRToCart
        public ActionResult AddToCart(int ProductId)
        {
            //determine cardId to group this user's item together
            string CurrentCartId;
            GetCartId();
            CurrentCartId = Session["CartId"].ToString();

            //if item is already in the user's cart then icrement by 1
            Cart cart = db.Carts.SingleOrDefault(c => c.ProductId == ProductId && c.CartId == CurrentCartId);

            if (cart == null)
            {
                //create and save the new cart item
                cart = new Cart
                {
                    CartId = CurrentCartId,
                    ProductId = ProductId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };
                db.Carts.Add(cart);
            }
            else
            {
                cart.Count++;
            }
            db.SaveChanges();

            //load the shopping cart page
            return RedirectToAction("ShoppingCart");
        }

        private void GetCartId()
        {
            //if there is no CartID session veriable(this is first item.)
            if (Session["CartId"] == null)
            {
                //is user loged in?
                if (User.Identity.IsAuthenticated)
                {
                    Session["CartId"] = User.Identity.Name;
                }
                else
                {
                    // so generate random number
                    Session["CartId"] = Guid.NewGuid().ToString();
                }
            }

        }
        //GET Storw/ShoppingCart
        public ActionResult ShoppingCart()
        {
            //check or generate cart id
            GetCartId();

            //select all the item in the current user's cart
            string CurrentCartId = Session["CartId"].ToString();
            var CartItems = db.Carts.Where(c => c.CartId == CurrentCartId).ToList();

            return View(CartItems);
        }

        //GET /Store/RemoveFromCart
        public ActionResult RemoveFromCart(int id)
        {
            //find and delete this record from user's cart
            Cart CartItem = db.Carts.SingleOrDefault(c => c.RecordId == id);
            db.Carts.Remove(CartItem);
            db.SaveChanges();

            //reload the updated cart page
            return RedirectToAction("ShoppingCart");
        }

        //GET Store/Checkout
        [Authorize]
        public ActionResult Checkout()
        {
            //migrate the cart to the username if shopping anonymous
            MigrateCart();
            return View();
        }

        //Post Store/Checkour
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(FormCollection values)
        {
            //create a new order and populate it from value
            Order order = new Order();
            TryUpdateModel(order);

            //autopopulate the field that we can 
            order.Username = User.Identity.Name;
            order.Email = User.Identity.Name;
            order.OrderDate = DateTime.Now;

            //get user's item and calc cart total
            var CartItems = db.Carts.Where(c => c.CartId == User.Identity.Name);
            decimal OrderTotal = (from c in CartItems
                                  select (int)c.Count * c.Product.Price).Sum();
            order.Total = OrderTotal;

            //save main order
            db.Orders.Add(order);
            db.SaveChanges();

            //now save each items
            foreach (Cart item in CartItems)
            {
                OrderDetail od = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Count,
                    UnitPrice = item.Product.Price
                };
                db.OrderDetails.Add(od);
            }

            //save all the order items
            db.SaveChanges();

            //show the recipt
            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }

        private void MigrateCart()
        {
            //attach anonymous cart to an authenticated once they log in
            if (!String.IsNullOrEmpty(Session["CartId"].ToString()) && User.Identity.IsAuthenticated)
            {
                if (Session["CartId"].ToString() != User.Identity.Name)
                {
                    //get cart item with random id
                    string CurrentCartId = Session["CartId"].ToString();
                    var CartItems = db.Carts.Where(c => c.CartId == CurrentCartId);

                    foreach (Cart item in CartItems)
                    {
                        item.CartId = User.Identity.Name;
                    }

                    db.SaveChanges();

                    //update the session variable to the username
                    Session["CartId"] = User.Identity.Name;
                }
            }
        }
    }
}
