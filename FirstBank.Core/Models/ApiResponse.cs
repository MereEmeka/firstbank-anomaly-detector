using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace FirstBank.Core.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}