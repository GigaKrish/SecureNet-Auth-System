using System;
using System.ComponentModel.DataAnnotations;

namespace RegistrationAppNoEF.Models
{
    public class User
    {
        public int Id { get; set; }

        
        public string Name { get; set; }

        
        public string UserName { get; set; }

        
        public string Email { get; set; }

        
        public string Password { get; set; }

        

        
        public int Age { get; set; }

        
        public string Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }


        [Required(ErrorMessage = "CAPTCHA is required")]
        public string CaptchaInput { get; set; }



       

    }
}