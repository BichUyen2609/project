// File: Extensions/EnumExtensions.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem

namespace HeThongTimViec.Extensions // Or your preferred namespace
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Lấy tên hiển thị (Display Name) của một giá trị enum.
        /// Trả về tên thành viên nếu không có DisplayAttribute.
        /// </summary>
        public static string GetDisplayName(this Enum value)
        {
            if (value == null) return string.Empty;

            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return value.ToString();

            var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>(false);
            return displayAttribute?.GetName() ?? value.ToString();
        }

        // --- Core Private Helper ---
        /// <summary>
        /// Logic cốt lõi để tạo danh sách SelectListItem từ một kiểu Enum.
        /// </summary>
        private static List<SelectListItem> GenerateSelectList<TEnum>(TEnum? selectedValue, bool includeDefaultItem, string defaultItemText, string defaultItemValue)
            where TEnum : struct, Enum
        {
            var enumType = typeof(TEnum);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("TEnum must be an Enum type.");
            }

            var items = new List<SelectListItem>();

            if (includeDefaultItem)
            {
                items.Add(new SelectListItem
                {
                    Text = defaultItemText,
                    Value = defaultItemValue,
                    Selected = !selectedValue.HasValue // Mục mặc định được chọn nếu không có giá trị cụ thể nào được chọn
                });
            }

            foreach (Enum value in Enum.GetValues(enumType))
            {
                items.Add(new SelectListItem
                {
                    Text = value.GetDisplayName(),
                    Value = value.ToString(),
                    Selected = selectedValue.HasValue && value.Equals(selectedValue.Value) // Đánh dấu mục được chọn nếu khớp
                });
            }
            return items;
        }

        // --- Public Extension Methods ---

        /// <summary>
        /// Tạo danh sách SelectListItem từ một giá trị enum (non-nullable).
        /// Giá trị được truyền vào sẽ được đánh dấu là 'selected'.
        /// </summary>
        public static List<SelectListItem> ToSelectList<TEnum>(this TEnum enumValue, // KHÔNG có giá trị mặc định cho 'this'
                                                            bool includeDefaultItem = false,
                                                            string defaultItemText = "-- Vui lòng chọn --",
                                                            string defaultItemValue = "")
            where TEnum : struct, Enum
        {
            // Gọi helper với giá trị hiện tại (ép kiểu sang nullable để dùng chung logic)
            return GenerateSelectList<TEnum>((TEnum?)enumValue, includeDefaultItem, defaultItemText, defaultItemValue);
        }

        /// <summary>
        /// Tạo danh sách SelectListItem từ một giá trị enum (nullable).
        /// Nếu giá trị là null, không có mục nào (ngoài mục default) được chọn.
        /// </summary>
        public static List<SelectListItem> ToSelectList<TEnum>(this TEnum? enumValue, // KHÔNG có giá trị mặc định cho 'this'
                                                            bool includeDefaultItem = false,
                                                            string defaultItemText = "-- Vui lòng chọn --",
                                                            string defaultItemValue = "")
             where TEnum : struct, Enum
        {
            // Gọi helper với giá trị nullable hiện tại
            return GenerateSelectList<TEnum>(enumValue, includeDefaultItem, defaultItemText, defaultItemValue);
        }

        // --- Public Static Method ---

        /// <summary>
        /// Tạo danh sách SelectListItem cho một kiểu Enum mà không cần một giá trị cụ thể ban đầu.
        /// Hữu ích cho form tạo mới (Create).
        /// </summary>
        public static List<SelectListItem> GetSelectList<TEnum>(bool includeDefaultItem = false,
                                                              string defaultItemText = "-- Vui lòng chọn --",
                                                              string defaultItemValue = "")
             where TEnum : struct, Enum
        {
             // Gọi helper với selectedValue là null
            return GenerateSelectList<TEnum>(null, includeDefaultItem, defaultItemText, defaultItemValue);
        }
    }
}