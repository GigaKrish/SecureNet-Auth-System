using RegistrationAppNoEF.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Web.Mvc;

namespace RegistrationAppNoEF.Controllers
{
    public class UserController : Controller
    {
        string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]


        public JsonResult Register(User user)
        {
            string sessionCaptcha = Convert.ToString(Session["Captcha"]);

            if(user.CaptchaInput != sessionCaptcha)
            {
                return Json(new { status = "error", message = "Invalid CAPTCHA.Please try Again." });
            }


            if (ModelState.IsValid)
            {
                // Step 1: Check for existing username or email
                bool userExists = false;
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    SqlCommand checkCmd = new SqlCommand("sp_CheckUserExists", conn);
                    checkCmd.CommandType = CommandType.StoredProcedure;
                    checkCmd.Parameters.AddWithValue("@UserName", user.UserName);
                    checkCmd.Parameters.AddWithValue("@Email", user.Email);

                    SqlParameter existsParam = new SqlParameter("@Exists", SqlDbType.Bit);
                    existsParam.Direction = ParameterDirection.Output;
                    checkCmd.Parameters.Add(existsParam);

                    conn.Open();
                    checkCmd.ExecuteNonQuery();

                    userExists = Convert.ToBoolean(existsParam.Value);
                }

                if (userExists)
                {
                    return Json(new { status = "error", message = "Username or Email already registered." });
                }








                // 2. Save data to database using SqlConnection + SqlCommand


                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_InsertUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        string encryptedPassword = StringCipher.Encrypt(user.Password);
                        cmd.Parameters.AddWithValue("@Name", user.Name);
                        cmd.Parameters.AddWithValue("@UserName", user.UserName);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        //cmd.Parameters.AddWithValue("@Password", user.Password);
                        cmd.Parameters.AddWithValue("@Password", encryptedPassword);
                        cmd.Parameters.AddWithValue("@Age", user.Age);
                        cmd.Parameters.AddWithValue("@Gender", user.Gender);
                        cmd.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }



                //ViewBag.Message = "Registration successful!";
                //// 3. Send confirmation email
                SendEmail(user.Email, user.UserName, user.Password);

                return Json(new { status = "success", redirectUrl = Url.Action("Login", "User") });
            }

                    return Json(new { status = "error",message = "Invalid data. Check Your Inputs." });
        }






        private void SendEmail(string toEmail, string username, string password)
        {
            string subject = "Registration Successful";
            string body = $@"
Username: {username}
Password: {password}

You have successfully registered.
Your credentials are:

Username: {username}
Password: {password}

URL: {Url.Action("Login", "User", null, Request.Url.Scheme)}

Thanks,
Team
";

            string smtpUser = ConfigurationManager.AppSettings["smtpUser"];
            string smtpPass = ConfigurationManager.AppSettings["smtpPass"];
            string ip = DeviceInfoHelper.GetIPAddress();
            string mac = DeviceInfoHelper.GetMacAddress();
            string cc = ""; // optional
            string bcc = ""; // optional

            var fromAddress = new MailAddress(smtpUser, "Your App");
            var toAddress = new MailAddress(toEmail);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                // Optionally add CC and BCC
                if (!string.IsNullOrEmpty(cc)) message.CC.Add(cc);
                if (!string.IsNullOrEmpty(bcc)) message.Bcc.Add(bcc);

                smtp.Send(message);

                // ✅ Insert into tbl_maillogs
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_InsertMailLog", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@To", toEmail);
                        cmd.Parameters.AddWithValue("@From", smtpUser);
                        cmd.Parameters.AddWithValue("@CC", cc);
                        cmd.Parameters.AddWithValue("@BCC", bcc);
                        cmd.Parameters.AddWithValue("@IP", ip);
                        cmd.Parameters.AddWithValue("@MAC", mac);
                        cmd.Parameters.AddWithValue("@Body", body);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }



                    //string query = @"
                    // INSERT INTO tbl_maillogs 
                    // ([To], [From], [Cc], [Bcc], [IPAddress], [MACAddress], [Body], [SendDate])
                    // VALUES 
                    // (@To, @From, @CC, @BCC, @IP, @MAC, @Body, @Date)";

                    //SqlCommand cmd = new SqlCommand(query, conn);
                    //cmd.Parameters.AddWithValue("@To", toEmail);
                    //cmd.Parameters.AddWithValue("@From", smtpUser);
                    //cmd.Parameters.AddWithValue("@CC", cc);
                    //cmd.Parameters.AddWithValue("@BCC", bcc);
                    //cmd.Parameters.AddWithValue("@IP", ip);
                    //cmd.Parameters.AddWithValue("@MAC", mac);
                    //cmd.Parameters.AddWithValue("@Body", body);
                    //cmd.Parameters.AddWithValue("@Date", DateTime.Now);

                    //conn.Open();
                    //cmd.ExecuteNonQuery();
                }
            }
        }








       
        


















        public void LogEmail(string to, string from, string cc, string bcc, string body)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string ip = DeviceInfoHelper.GetIPAddress();
            string mac = DeviceInfoHelper.GetMacAddress();
            DateTime now = DateTime.Now;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
             //   string query = @"
            //INSERT INTO tbl_maillogs 
            //([From], [To], [Cc], [Bcc], [IPAddress], [MacAddress], [SendDate], [Body])
            //VALUES 
            //(@From, @To, @Cc, @Bcc, @IP, @Mac, @Date, @Body)";

                using (SqlCommand cmd = new SqlCommand("usp_InsertMailLog", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    cmd.Parameters.AddWithValue("@Cc", cc ?? "");
                    cmd.Parameters.AddWithValue("@Bcc", bcc ?? "");
                    cmd.Parameters.AddWithValue("@IP", ip);
                    cmd.Parameters.AddWithValue("@Mac", mac);
                    cmd.Parameters.AddWithValue("@Date", now);
                    cmd.Parameters.AddWithValue("@Body", body);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }




        [HttpGet]
        public JsonResult GetUsers()
        {
            List<User> users = new List<User>();
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT * FROM Users";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)rdr["Id"],
                        Name = rdr["Name"].ToString(),
                        UserName = rdr["UserName"].ToString(),
                        Email = rdr["Email"].ToString(),
                        Password = rdr["Password"].ToString(),
                        Age = (int)rdr["Age"],
                        Gender = rdr["Gender"].ToString(),
                        DateOfBirth = (DateTime)rdr["DateOfBirth"]
                    });
                }
            }
            return Json(users, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public ActionResult Login()
        {
            // Generate 6-character random CAPTCHA
            string captcha = GenerateCaptchaCode();
            Session["Captcha"] = captcha;

            ViewBag.Captcha = captcha;
            return View();
        }

        public JsonResult RefreshCaptcha()
        {
            string Captcha = GenerateCaptchaCode();
            Session["Captcha"] = Captcha;
            return Json(new { captcha = Captcha }, JsonRequestBehavior.AllowGet);
        }

        private string GenerateCaptchaCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }






        [HttpPost]
        public ActionResult Login(string email, string password,string CaptchaInput)
        {



            ///
            string sessionCaptcha = Session["Captcha"]?.ToString();

            if (CaptchaInput != sessionCaptcha)
            {
                ViewBag.Message = "Invalid CAPTCHA. Please try again.";
                //here
                ViewBag.Captcha = GenerateCaptchaCode(); // regenerate on failure
                Session["Captcha"] = ViewBag.Captcha;
                //here
                return View();
            }

            ////


            ///////////////////////////////////before we used///


            //User user = null;
            //using (SqlConnection con = new SqlConnection(connStr))



            //{
            //    string query = "SELECT * FROM Users WHERE Email = @Email AND Password = @Password";
            //    SqlCommand cmd = new SqlCommand(query, con);
            //    cmd.Parameters.AddWithValue("@Email", email);
            //    cmd.Parameters.AddWithValue("@Password", password);
            //    con.Open();
            //    SqlDataReader rdr = cmd.ExecuteReader();
            //    if (rdr.Read())
            //    {
            //        user = new User
            //        {
            //            Id = (int)rdr["Id"],
            //            Name = rdr["Name"].ToString(),
            //            UserName = rdr["UserName"].ToString(),
            //            Email = rdr["Email"].ToString(),
            //            Password = rdr["Password"].ToString(),
            //            Age = (int)rdr["Age"],
            //            Gender = rdr["Gender"].ToString(),
            //            DateOfBirth = (DateTime)rdr["DateOfBirth"]
            //        };
            //    }
            //}

            //if (user != null)
            //{
            //    Session["User"] = user; // Save user in session
            //    return RedirectToAction("Dashboard");
            //}

            //ViewBag.Message = "Invalid email or password";
            ////here
            //ViewBag.Captcha = GenerateCaptchaCode(); // regenerate on failure
            //Session["Captcha"] = ViewBag.Captcha;
            ////here
            //return View();


            /////////////////////////////////////////////////////////////////////////
            ///

            ///// new one ////
            //User user = null;
            //string encryptedPassword = null;

            //using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            //{
            //    string query = "SELECT * FROM Users WHERE Email = @Email";
            //    SqlCommand cmd = new SqlCommand(query,conn);
            //    cmd.Parameters.AddWithValue("@Email",email);
            //    conn.Open();

            //    SqlDataReader rdr = cmd.ExecuteReader();
            //    if (rdr.Read())
            //    {
            //        encryptedPassword = rdr["Password"].ToString(); //to get the encryption password

            //        user = new User
            //        {
            //            Id = Convert.ToInt32(rdr["ID"]),
            //            Name = rdr["Name"].ToString(),
            //            UserName = rdr["UserName"].ToString(),
            //            Email = rdr["Email"].ToString(),
            //            Password = encryptedPassword,
            //            Age = Convert.ToInt32(rdr["Age"]),
            //            Gender = rdr["Gender"].ToString(),
            //            DateOfBirth = (DateTime)rdr["DateOfBirth"]
            //        };
            //    }

            //}


            User user = null;
            string encryptedPassword = null;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("usp_GetUserByEmail", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);

                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    encryptedPassword = rdr["Password"].ToString(); // get encrypted password

                    user = new User
                    {
                        Id = Convert.ToInt32(rdr["ID"]),
                        Name = rdr["Name"].ToString(),
                        UserName = rdr["UserName"].ToString(),
                        Email = rdr["Email"].ToString(),
                        Password = encryptedPassword,
                        Age = Convert.ToInt32(rdr["Age"]),
                        Gender = rdr["Gender"].ToString(),
                        DateOfBirth = (DateTime)rdr["DateOfBirth"]
                    };
                }
            }


            //Now to check the password

            if (!string.IsNullOrEmpty(encryptedPassword))
            {
                string decryptedPassword = StringCipher.Decrypt(encryptedPassword);

                if (decryptedPassword == password && user != null)
                {
                    Session["User"] = user;
                    return RedirectToAction("Dashboard");
                }
            }

            ViewBag.Message = "Invalid credentials";
            ViewBag.Captcha = GenerateCaptchaCode(); // regenerate on failure
            Session["Captcha"] = ViewBag.Captcha;
            return View();


        }

        public ActionResult Dashboard()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];
            return View(user);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult GetCaptchaText()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            var captcha = new string(Enumerable.Repeat(chars,6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            Session["Captcha"] = captcha;
            return Json(new {captcha}, JsonRequestBehavior.AllowGet);
        }




        //////////ForgetPassword/////////
        [HttpPost]
        public JsonResult VerifyUsername(string userName)
        {
            
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                //string query = "SELECT COUNT(*) FROM Users WHERE UserName = @UserName";

                //SqlCommand cmd = new SqlCommand("query", conn);
                //cmd.Parameters.AddWithValue("@UserName", userName);
                //conn.Open();
                //int count = (int)cmd.ExecuteScalar();
                //return Json(new { status = count > 0 ? "valid" : "invalid" }, JsonRequestBehavior.AllowGet);

                //using (SqlConnection conn = new SqlConnection("YourConnectionString"))
                int userCount = 0;
                using (SqlCommand cmd = new SqlCommand("usp_verifiedUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@UserName", userName);

                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            userCount = Convert.ToInt32(result);
                        }
                    }
                return Json(new { status = userCount > 0 ? "valid" : "invalid" }, JsonRequestBehavior.AllowGet);


            }
        }



        public ActionResult ForgotPassword()
        {  
            return View(); 
        }



        ///////////////////////////Reset password///

        //[HttpPost]
        //public JsonResult ResetPassword(string userName, string newPassword)
        //{
        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
        //    {
        //        string checkQuery = "SELECT Password FROM Users WHERE UserName = @UserName";
        //        SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
        //        checkCmd.Parameters.AddWithValue("@UserName", userName);
        //        conn.Open();

        //        string existingPassword = Convert.ToString(checkCmd.ExecuteScalar());

        //        if (existingPassword == newPassword)
        //        {
        //            return Json(new { status = "error", message = "New password must be different from the old one." });
        //        }

        //        string updateQuery = "UPDATE Users SET Password = @Password WHERE UserName = @UserName";
        //        SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
        //        updateCmd.Parameters.AddWithValue("@Password", newPassword);
        //        updateCmd.Parameters.AddWithValue("@UserName", userName);
        //        updateCmd.ExecuteNonQuery();

        //        return Json(new { status = "success", redirectUrl = Url.Action("Login", "User") }, JsonRequestBehavior.AllowGet);
        //    }
        //}

                            //////RESETpassword with encryption///////
        

        //[HttpPost]
        //public JsonResult ResetPassword(string username, string newPassword)
        //{
        //    string existingEncryptedPassword = null;

        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
        //    {
        //        string query = "SELECT Password FROM Users WHERE UserName = @UserName";
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@UserName", username);
        //        conn.Open();
        //        var result = cmd.ExecuteScalar();
        //        if (result != null)
        //        {
        //            existingEncryptedPassword = result.ToString();
        //        }
        //    }

        //    if (existingEncryptedPassword != null && StringCipher.Decrypt(existingEncryptedPassword) == newPassword)
        //    {
        //        return Json(new { status = "error", message = "New password must be different from the old password." });
        //    }

        //    string newEncryptedPassword = StringCipher.Encrypt(newPassword);

        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
        //    {
        //        string query = "UPDATE Users SET Password = @Password WHERE UserName = @UserName";
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@Password", newEncryptedPassword);
        //        cmd.Parameters.AddWithValue("@UserName", username);
        //        conn.Open();
        //        cmd.ExecuteNonQuery();
        //    }

        //    return Json(new { status = "success", redirectUrl = Url.Action("Login", "User") });
        //}





        //////////////////////////////////for otp verification//////////
        



        [HttpPost]
        public JsonResult SendOtp(string username)
        {
            string email = null;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                string query = "SELECT Email FROM Users WHERE UserName = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    email = result.ToString();
                }
            }

            if (!string.IsNullOrEmpty(email))
            {
                // Generate 6-digit OTP
                string otp = new Random().Next(100000, 999999).ToString();
                Session["OTP"] = otp;
                Session["OTPEmail"] = email;

                // Send email
                string subject = "Your OTP for Password Reset";
                string body = $"Your OTP code is: {otp}";
                EmailHelper.SendMail(email, subject, body);  // Use your helper

                return Json(new { status = "success", message = "OTP sent to registered email." });
            }

            return Json(new { status = "error", message = "Username not found." });
        }

        // Step 3: Verify OTP
        [HttpPost]
        public JsonResult VerifyOtp(string otp)
        {
            if (Session["OTP"] != null && Session["OTP"].ToString() == otp)
            {
                return Json(new { status = "success" });
            }
            return Json(new { status = "error", message = "Invalid OTP." });
        }










        // Step 4: Update Password
        [HttpPost]
        public JsonResult ResetPassword(string newPassword)
        {
            if (Session["OTPEmail"] == null)
                return Json(new { status = "error", message = "Session expired." });

            string email = Session["OTPEmail"].ToString();
            string existingEncryptedPassword = null;

            // Step 4.1: Get encrypted old password from DB
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                string query = "SELECT Password FROM Users WHERE Email = @Email";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    existingEncryptedPassword = result.ToString();
                }
            }

            // Step 4.2: Compare decrypted old password with new
            if (!string.IsNullOrEmpty(existingEncryptedPassword))
            {
                string oldPassword = StringCipher.Decrypt(existingEncryptedPassword);
                if (oldPassword == newPassword)
                {
                    return Json(new { status = "error", message = "New password must be different from the old password." });
                }
            }

            // Step 4.3: Encrypt new password
            string encryptedNewPassword = StringCipher.Encrypt(newPassword);

            // Step 4.4: Update new password in DB
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                string query = "UPDATE Users SET Password = @Password WHERE Email = @Email";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Password", encryptedNewPassword);
                cmd.Parameters.AddWithValue("@Email", email);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // Step 4.5: Send confirmation mail
            EmailHelper.SendMail(email, "Password Changed", $"Your new password is: {newPassword}");

            // Step 4.6: Clear session
            Session["OTP"] = null;
            Session["OTPEmail"] = null;

            return Json(new { status = "success", message = "Password changed successfully.", redirectUrl = Url.Action("Login", "User") });
        }




        //////// EmailHelper//////

        public static class EmailHelper
        {
            public static void SendMail(string to, string subject, string body)
            {
                string from = ConfigurationManager.AppSettings["smtpUser"];
                string pass = ConfigurationManager.AppSettings["smtpPass"];

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(from, pass)
                };

                var message = new MailMessage(from, to, subject, body);
                smtp.Send(message);
            }
        }

    }
}