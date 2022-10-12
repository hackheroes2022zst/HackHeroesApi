using Microsoft.AspNetCore.Mvc;
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


    int checkData(userData reg)
        {
            if (reg.Name.Length < 3) return 1; else if (reg.Name.Length > 30) return 2; // 1 - short name, 2 - long name

            if (reg.Name.Any(ch => !Char.IsLetterOrDigit(ch))) return 3; // 3 - Name contains special characters

            if (reg.Surname.Length < 3) return 4; else if (reg.Surname.Length > 30) return 5; // 4 - short surname, 5 - long surname

            if (reg.Surname.Any(ch => !Char.IsLetterOrDigit(ch))) return 6; // Surname contains special characters

            if (reg.Password.Length < 8) return 7; else if (reg.Password.Length > 30) return 8; // 7 - password too short, 8 - password too long

            if (!new EmailAddressAttribute().IsValid(reg.Email)) return 9; // Email not valid

            
            return 0; // Check passed
        }

        //Register endpoint
        [HttpPost]
        [Route("register")]
        public string Register(userData register)
        {
            if (register != null)
            {
                // Checking if data is correctly assigned

                int checkInfo = checkData(register);
                switch(checkInfo)
                {
                    case 1: return "Name is too short"; case 2: return "Name is too long"; // Name
                    case 3: return "Name contains special characters"; // Name /w special chars
                    case 4: return "Surname is too short"; case 5: return "Surname is too long"; // Surname
                    case 6: return "Surname contains special characters"; // Surname /w special chars
                    case 7: return "Password is too short"; case 8: return "Password is too long"; // Password
                    case 9: return "Email is not valid"; // Email validation
                    default: break; // Check pass
                }
                
                //Database connection
                using (MySqlConnection db = new(_config.GetConnectionString("LogReg").ToString()))
                {
                    db.Open();

                    //Checking if email already exists in database
                    MySqlDataAdapter emailcheck = new($"SELECT * FROM userData WHERE email = '{register.Email}'", db);
                    DataTable check = new();
                    emailcheck.Fill(check);
                    if (check.Rows.Count > 0) return "A account with that email already exists";

                    //MySql query
                    MySqlCommand query = new MySqlCommand($"INSERT INTO userData(uuid, name, surname, email, password, gender, city, adress, phone) VALUES (UUID(), '{register.Name}', '{register.Surname}', '{register.Email}', '{Encrypt(register.Password)}', '{register.Gender}','{register.City}', '{register.Address}', '{register.Phone}')", db);
                    int response = query.ExecuteNonQuery();
                    if (response == 1)
                    {
                        return "Succesfully registered";
                    }
                    else return "There was an error while registering";

                }
            }
            else return "ERROR, NO DATA"; // Happens when data passed from front-end doesn't exist




        }

        //Login endpoint
        [HttpPost]
        [Route("login")]

        public string Login(userData login)
        {
            //Database connection
            using (MySqlConnection db = new(_config.GetConnectionString("LogReg").ToString()))
            {
                db.Open();

                //MySql query
                MySqlDataAdapter loginQuery = new($"SELECT * FROM userData WHERE email = '{login.Email}' AND password = '{Encrypt(login.Password)}'", db);
                DataTable loginDT = new();
                loginQuery.Fill(loginDT);
                
                if (loginDT.Rows.Count > 0)
                {
                    return "Logged in succesfully";
                }
                else
                {
                    return "Incorrect email or password";
                }
            }
        }

        //Application creation endpoint
        [HttpPost]
        [Route("createApplication")]

        public string CreateApplication(applicationData create)
        {
            return "NotCreated";
        }
    }
}
