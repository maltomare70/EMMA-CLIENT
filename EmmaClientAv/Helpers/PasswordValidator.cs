
using System;
using System.Text.RegularExpressions;

namespace EmmaClientAv.Helpers;



public class PasswordValidator
{
    public static bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;

        // La regex per la validazione
        string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        return Regex.IsMatch(password, pattern);
    }
}