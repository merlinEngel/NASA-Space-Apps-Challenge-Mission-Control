using System;
using Newtonsoft.Json.Serialization;

namespace MissionCore.Text
{
    public static class EnumExtensions
    {
        static readonly SnakeCaseNamingStrategy snake = new();

        public static string ToSnakeCase(this Enum value) =>
            snake.GetPropertyName(value.ToString(), false);
    }
}