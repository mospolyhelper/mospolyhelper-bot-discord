using System;

namespace Mospolyhelper.Domain.Schedule.Models
{
    public class Auditorium
    {
        public string Title { get; }
        public string Color { get; }

        public Auditorium(string title, string color)
        {
            this.Title = title;
            this.Color = color;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Auditorium other)
            {
                return Equals(other);
            }
            else
            {
                return false;
            }
        }

        protected bool Equals(Auditorium other)
        {
            return Title == other.Title && Color == other.Color;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Color);
        }
    }

    class AuditoriumExt
    {
        public static string ReplaceEmojiByText(string raw)
        {
            if (raw.Contains("\uD83D\uDCF7"))
                return raw.Replace("\uD83D\uDCF7", "(Вебинар)"); // 📷
            if (raw.Contains("\uD83C\uDFE0"))
                return raw.Replace("\uD83C\uDFE0", "(LMS)"); // 🏠
            return raw;

        }
    }
}
