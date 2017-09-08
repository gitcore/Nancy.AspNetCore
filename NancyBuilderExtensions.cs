using Nancy.AspNetCore;
using System;

namespace Microsoft.AspNetCore.Builder {
    public static class NancyBuilderExtensions {
        /// <summary>
        /// Adds Nancy to the AspNetCore pipeline.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="action">A configuration builder action.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseNancy(this IApplicationBuilder builder, Action<NancyOptions> action) {
            var options = new NancyOptions();

            action(options);

            return builder.UseNancy(options);
        }

        /// <summary>
        /// Adds Nancy to the AspNetCore pipeline.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="options">The Nancy options.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseNancy(this IApplicationBuilder builder, NancyOptions options = null) {
            var nancyOptions = options ?? new NancyOptions();

            builder.UseMiddleware<NancyMiddleware>(nancyOptions);

            return builder;
        }
    }
}
