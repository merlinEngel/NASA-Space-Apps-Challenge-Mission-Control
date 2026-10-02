using System;
using Newtonsoft.Json.Serialization;

namespace MissionCore
{
    public static class EnumExtensions
    {
        static readonly SnakeCaseNamingStrategy snake = new();

        public static string ToSnakeCase(this Enum value) =>
            snake.GetPropertyName(value.ToString(), false);
    }
}