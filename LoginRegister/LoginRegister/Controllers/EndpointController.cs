using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ApiEndPoints.Controllers
{
    [Route("api/[controller]")]
    [ApiController]


    public class EndpointController : ControllerBase
    {
        private readonly IConfiguration _config;

        public EndpointController(IConfiguration cfg)
        {
            _config = cfg;
        }


        // getHash function for Encryption, returns the 64 bit SHA512 hash
        public static byte[] getHash(string pass)
        {
            using (HashAlgorithm hash = SHA512.Create()) return hash.ComputeHash(Encoding.UTF8.GetBytes(pass));
        }

        // Encryption function for password safety
        public static string Encrypt(string pass)
        {
            StringBuilder builder = new StringBuilder();

            foreach (byte x in getHash(pass)) builder.Append(x.ToString("X2"));

            return builder.ToString();
        }


        int checkUserData(userData ud)
        {

            if (ud.Password == null ||
                ud.Email == null ||
                ud.Name == null ||
                ud.Surname == null) return -1; // DATA ERROR

            if (ud.Name.Length < 3) return 1; else if (ud.Name.Length > 30) return 2; // 1 - short name, 2 - long name

            if (ud.Name.Any(ch => !Char.IsLetterOrDigit(ch))) return 3; // 3 - Name contains special characters

            if (ud.Surname.Length < 3) return 4; else if (ud.Surname.Length > 30) return 5; // 4 - short surname, 5 - long surname

            if (ud.Surname.Any(ch => !Char.IsLetterOrDigit(ch))) return 6; // Surname contains special characters

            if (ud.Password.Length < 8) return 7; else if (ud.Password.Length > 30) return 8; // 7 - password too short, 8 - password too long

            if (!new EmailAddressAttribute().IsValid(ud.Email)) return 9; // Email not valid


            return 0; // Check passed
        }

        int checkAppData(applicationData ad)
        {
            if (ad.name == null ||
                ad.userId == null ||
                ad.createdBy == null ||
                ad.category == null ||
                ad.reward == null) return 1;
            return 0;
        }

        // Database connection
        MySqlConnection getConnection()
        {
            return new(_config.GetConnectionString("LogReg").ToString());
        }


        // Register endpoint
        [HttpPost]
        [Route("register")]

        public string Register(userData register)
        {
            if (register != null)
            {
                // Checking if data is correctly assigned

                int checkInfo = checkUserData(register);
                switch (checkInfo)
                {
                    case -1: return "DATA ERROR. DATA INCORRECTLY ASSIGNED";
                    case 1: return "Name is too short";
                    case 2: return "Name is too long"; // Name
                    case 3: return "Name contains special characters"; // Name /w special chars
                    case 4: return "Surname is too short";
                    case 5: return "Surname is too long"; // Surname
                    case 6: return "Surname contains special characters"; // Surname /w special chars
                    case 7: return "Password is too short";
                    case 8: return "Password is too long"; // Password
                    case 9: return "Email is not valid"; // Email validation
                    default: break; // Check pass
                }

                // Database connection
                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    // Checking if email already exists in database
                    MySqlDataAdapter emailcheck = new($"SELECT * FROM userData WHERE email = '{register.Email}'", db);
                    DataTable check = new();
                    emailcheck.Fill(check);
                    if (check.Rows.Count > 0) return "A account with that email already exists";

                    // MySql query
                    MySqlCommand query = new MySqlCommand($"INSERT INTO userData(uuid, name, surname, email, password, gender, city, adress, phone) VALUES (UUID(), '{register.Name}', '{register.Surname}', '{register.Email}', '{Encrypt(register.Password)}', '{register.Gender}','{register.City}', '{register.Address}', '{register.Phone}')", db);
                    int queryResult = query.ExecuteNonQuery();
                    if (queryResult == 1) return "Succesfully registered";
                    else return "There was an error while registering";

                }
            }
            else return "DATA ERROR. NO DATA PASSED"; // Happens when data passed from front-end doesn't exist




        }

        // Login endpoint
        [HttpPost]
        [Route("login")]

        public string Login(userData login)
        {
            if (login != null)
            {

                //Database connection
                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    //MySql query
                    MySqlDataAdapter loginQuery = new($"SELECT * FROM userData WHERE email = '{login.Email}' AND password = '{Encrypt(login.Password)}'", db);
                    DataTable loginDT = new();
                    loginQuery.Fill(loginDT);

                    if (loginDT.Rows.Count > 0) return "Logged in succesfully";
                    else return "Incorrect email or password";

                }
            }
            return "DATA ERROR. NO DATA PASSED"; //  Happens when data passed from front-end doesn't exist
        }

        // Application creation endpoint
        [HttpPost]
        [Route("createApplication")]

        public string CreateApplication(applicationData create)
        {
            if (create != null)
            {
                // Data check if any values that aren't intended to, are null
                int checkData = checkAppData(create);

                if (checkData == 1) return "DATA ERROR. DATA INCORRECTLY ASSIGNED";

                using (MySqlConnection db = getConnection())
                {

                    MySqlDataAdapter check = new($"SELECT * FROM application WHERE userId = '{create.userId}'", db);
                    DataTable data = new();
                    check.Fill(data);
                    if (data.Rows.Count > 0) return "You already have a published post!";


                    db.Open();

                    // MySql query
                    MySqlCommand query = new MySqlCommand($"INSERT INTO application(date_created, name, userId, applicationId, createdBy, category, reward, stake, description) VALUES (NOW(),'{create.name}', '{create.userId}',UUID() , '{create.createdBy}', '{create.category}', '{create.reward}','{create.stake}', '{create.description}')", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Created";
                    else return "Error with creating application";

                }

            }
            return "DATA ERROR. NO DATA PASSED"; //  Happens when data passed from front-end doesn't exist
        }

        // Application update endpoint
        [HttpPut]
        [Route("updateApplication")]

        public string updateApplication(applicationData update)
        {
            if (update != null)
            {
                // Data check 
                int checkData = checkAppData(update);

                if (checkData == 1) return "DATA ERROR. DATA INCORRECTLY ASSIGNED";

                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    // Replaces all data with new data
                    // MySql query
                    MySqlCommand query = new MySqlCommand($"UPDATE application SET name = '{update.name}', category = '{update.category}', reward = {update.reward}, stake = '{update.stake}', finished = '{update.finished}',  description = '{update.description}' WHERE userId = '{update.userId}'", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Updated application";
                    else return "Error with updating application";

                }
            }
            return "DATA ERROR. NO DATA PASSED"; //  Happens when data passed from front-end doesn't exist
        }

        // Application delete endpoint
        [HttpDelete]
        [Route("deleteApplication")]

        public string deleteApplication(applicationData delete)
        {
            if (delete != null)
            {

                if (delete.userId == null) return "DATA ERROR. DATA INCORRECTLY ASSIGNED";

                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    // MySql query
                    MySqlCommand query = new MySqlCommand($"DELETE FROM application WHERE userId = '{delete.userId}'", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Deleted application";
                    else return "Error with deleting application";
                }
            }
            return "DATA ERROR. NO DATA PASSED"; //  Happens when data passed from front-end doesn't exist
        }

        // Application get endpoint
        [HttpGet]
        [Route("getApplications")]

        public string getApplications()
        {
            using (MySqlConnection db = getConnection())
            {
                db.Open();

                MySqlDataAdapter query = new($"SELECT * FROM application WHERE finished = 0", db);
                DataTable data = new();
                query.Fill(data);
                string msg = "";

                if (data.Rows.Count > 0)
                {

                    for(int i = 0; i < data.Rows.Count; i++)
                    {
                        msg += data.Rows[i];
                    }

                }
                return msg;

            }
        }
    }
}
