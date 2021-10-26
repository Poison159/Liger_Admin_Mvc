using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using WebOneApp.Models;

namespace WebOneApp.Controllers.API
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class UsersController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET api/users/x
        public IHttpActionResult Get(int userId)
        {
            //var user = db.AppUsers.First(x => x.Id == userId);
            ////Get User cards
            //var cards = db.Resturants.Where(x => x.userId == user.Id).ToList();

            //List<Merchant> merchants = new List<Merchant>();

            //foreach (var card in cards)
            //{
            //    //Get Merchant for each card
            //    var merchant = db.Merchants.Where(x => x.Id == card.merchantId).FirstOrDefault();

            //    merchants.Add(merchant);
            //}
            //cards.Reverse();
            //merchants.Reverse();
            //CardRepresentative userCard = new CardRepresentative()
            //{
            //    User = user,
            //    Cards = cards,
            //    Merchants = merchants
            //};

            return Ok();


        }



        [Route("api/RegisterUser")]
        [HttpPost]
        public object RegisterUser([FromBody]FormDetails form)
        {
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            UserManager.UserValidator = new UserValidator<ApplicationUser>(UserManager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            var user = new ApplicationUser();

            user.Email = form.email.ToLower().Trim();
            user.UserName = form.email.ToLower().Trim();
            var result = UserManager.Create(user, form.password);
            var appUser = new AppUser() { Name = form.name, Email = form.email.ToLower().Trim(), MobileNumber = "0" };
            if (result.Succeeded)
            {
                var token = Helper.saveAppUserAndToken(appUser, db);
                return new { _token = token, _user = appUser };
            }
            else
            {
                return result;
            }
        }
        [Route("api/ChangeName")]
        [HttpGet]
        public bool changeName(int userId, string name)
        {
            try
            {
                var user = db.AppUsers.Find(userId);
                user.Name = name;
                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        [Route("api/CheckToken")]
        [HttpPost]
        public object getToken([FromBody]FormDetails form)
        {
            var userStore = new UserStore<IdentityUser>();
            var userManager = new UserManager<IdentityUser>(userStore);
            ApplicationUser user = null;
            AppUser appUser = null;

            try
            {
                appUser = db.AppUsers.First(x => x.Email.ToLower().Trim() == form.email.ToLower().Trim());
                user = db.Users.First(x => x.Email.ToLower().Trim() == form.email.ToLower().Trim());
                if (String.IsNullOrEmpty(appUser.MobileNumber))
                {
                    appUser.MobileNumber = "0";
                }
            }
            catch (Exception)
            {
                return new { Errors = "User not found" };
            }
            if (user != null)
            {
                var isMatch = userManager.CheckPassword(user, form.password);
                if (isMatch)
                {
                    var token = db.Tokens.ToList().First(x => x._userId.ToLower().Trim() == form.email.ToLower().Trim());
                    if (token != null)
                    {
                        token._grantDate = DateTime.Now;
                        token._expiryDate = DateTime.Now.AddDays(60);
                        db.SaveChanges();
                        return new { _token = token, _user = appUser };
                    }
                    else
                    {
                        return new { Errors = "User not found" };
                    }
                }
                else
                {
                    return new { Errors = "Incorrect cridentials" };
                }
            }
            else
            {
                return new { Errors = "User not found" };
            }
        }

        [Route("api/UserLogin")]
        [HttpPost]
        public IHttpActionResult UserLogin([FromBody]AppUser formUser)
        {
            //Check if user exists
            var encryptedPassword = EncryptPassword(formUser.Password);

            var user = db.AppUsers.Where(x => x.Password == encryptedPassword).FirstOrDefault();

            if (user is null)
                //User doesn't exist
                return NotFound();

            //Check for changes
            if (user.Name != formUser.Name || user.Email != formUser.Email || user.MobileNumber != formUser.MobileNumber)
            {
                user.Name = formUser.Name;
                user.Email = formUser.Email;
                user.MobileNumber = formUser.MobileNumber;
            }

            db.SaveChanges();

            user.Password = null;

            return Ok(user);
        }

        [Route("api/AddManager")]
        [HttpPost]
        public bool AddManager([FromBody]Manager man)
        {
            try
            {
                Helper.createManager(man.email, man.password, man.role, db);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // PUT api/users/x
        public IHttpActionResult Put(AppUser user)
        {
            if (!ModelState.IsValid)
                return BadRequest("Not a valid model");

            var existingUser = db.AppUsers
                                        .Where(s => s.Id == user.Id)
                                        .FirstOrDefault<AppUser>();

            if (existingUser != null)
            {
                existingUser.MobileNumber = user.MobileNumber;
                existingUser.Email = user.Email;

                db.SaveChanges();
            }
            else
                return NotFound();

            return Ok();
        }

        // DELETE api/users/5
        public IHttpActionResult Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Not a valid User id");

            var user = db.AppUsers
                                 .Where(s => s.Id == id)
                                 .FirstOrDefault();

            db.Entry(user).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();

            return Ok();
        }

        [Route("api/UserReservations")]
        [HttpGet]
        public List<Reservation> AddReservation(string email, int branchId)
        {
            //Get existing Reservations --> Approved or Awaiting
            var reservations = db.Reservations.ToList().Where(x => x.email == email && x.branchId == branchId && (x.status != "Rejected" || x.status != "Cancelled") && x.dateReservedAt > DateTime.Now.Date).ToList();

            var retList = new List<Reservation>();
            foreach (var item in reservations)
            {
                retList.Add(item);
            }
            return retList;
        }

        public string EncryptPassword(string password)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(password);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            String encryptedPasswordHash = System.Text.Encoding.ASCII.GetString(data);

            return encryptedPasswordHash;
        }
    }
}