using Microsoft.AspNetCore.Mvc;

namespace crmHuman.Helpers
{
    /// <summary>
    /// Helper class for standardizing API responses
    /// </summary>
    public static class ApiResponseHelper
    {
        /// <summary>
        /// Returns a success response
        /// </summary>
        public static IActionResult Success(object? data = null)
        {
            var response = new
            {
                success = true,
                data = data
            };
            return new JsonResult(response)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Returns a success response with custom object
        /// </summary>
        public static IActionResult SuccessResponse(object responseObject)
        {
            return new JsonResult(responseObject)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Returns a bad request response with validation errors
        /// </summary>
        public static IActionResult BadRequest(List<object> errors)
        {
            return new JsonResult(errors)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        /// <summary>
        /// Returns a bad request response with single error
        /// </summary>
        public static IActionResult BadRequest(string fieldName, string message)
        {
            var errors = new List<object>
            {
                new { name = fieldName, Content = message }
            };
            return BadRequest(errors);
        }

        /// <summary>
        /// Returns a not found response
        /// </summary>
        public static IActionResult NotFound(string message = "Không tìm thấy dữ liệu")
        {
            var response = new
            {
                success = false,
                message = message
            };
            return new JsonResult(response)
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        /// <summary>
        /// Returns an error response
        /// </summary>
        public static IActionResult Error(string message, int statusCode = StatusCodes.Status500InternalServerError)
        {
            var response = new
            {
                success = false,
                message = message
            };
            return new JsonResult(response)
            {
                StatusCode = statusCode
            };
        }
    }
}

