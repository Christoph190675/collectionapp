using System.Globalization;

namespace CollectionApp.Domain.Utils
{
    public static class BoolExtension
    {
        /// <summary>
        /// Convert a text to bool
        /// </summary>
        /// <param name="text">A string</param>
        /// <returns>A bool</returns>
        public static bool ToBool(this string text)
        {
            bool result = false;

            if (text != null)
            {
                switch (text.ToLower())
                {
                    case "1":
                    case "true":
                        result = true;
                        break;
                    default:

                        if (int.TryParse(text, out int resultInt))
                        {
                            result = resultInt > 0;
                        }
                        else if (double.TryParse(text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double resultDouble))
                        {
                            result = resultDouble > 0;
                        }
                        else
                        {
                            result = false;
                        }
                        break;
                }

            }
            return result;
        }
    }
}
