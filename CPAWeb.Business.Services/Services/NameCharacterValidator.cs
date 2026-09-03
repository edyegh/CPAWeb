using System;
using System.Globalization;

namespace CPAWeb.Business.Services.Services
{
    // cpa_service_ident-ում գրանցվող անունը պետք է լինի միայն լատինատառ (ASCII):
    // Հայկական '․' (U+2024), 'օ', ռուսերեն 'о' և նմանատիպ նիշերը տեսքով նույնն են,
    // ինչ անգլերենինը, բայց բազայում առաջացնում են սխալ, չորոնվող անուն:
    public static class NameCharacterValidator
    {
        // Թույլատրվում են միայն տպվող ASCII նիշերը՝ space-ից (0x20) մինչև '~' (0x7E)
        private const char MinAllowed = ' ';
        private const char MaxAllowed = '~';

        // Վերադարձնում է սխալի տեքստը, կամ null՝ եթե անունը վավեր է
        public static string? Validate(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c >= MinAllowed && c <= MaxAllowed)
                    continue;

                return $"'{name}' contains a non-english character {Describe(c)} at position {i + 1} — " +
                        "only latin letters, digits and basic punctuation are allowed.";
            }

            return null;
        }

        // Անտեսանելի նիշերի դեպքում ցույց ենք տալիս միայն Unicode կոդը
        private static string Describe(char c)
        {
            string code = "U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);

            return char.IsControl(c) || char.IsWhiteSpace(c)
                ? $"({code})"
                : $"'{c}' ({code})";
        }
    }
}
