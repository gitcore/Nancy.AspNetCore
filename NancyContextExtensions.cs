using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Nancy.AspNetCore {
    /// <summary>
    /// AspNetCore extensions for the NancyContext.
    /// </summary>
    public static class NancyContextExtensions {
        /// <summary>
        /// Gets the AspNetCore environment context.
        /// </summary>
        /// <param name="context">The Nancy context.</param>
        /// <returns>The AspNetCore environment context.</returns>
        public static HttpContext GetAspNetCoreEnvironment(this NancyContext context) {
            return (context.Items.ContainsKey(NancyMiddleware.AspNetCoreEnvironmentKey) ? 
                context.Items[NancyMiddleware.AspNetCoreEnvironmentKey] as HttpContext : null);
        }

        /// <summary>
        ///  Gets the Nancy environment context.
        /// </summary>
        /// <param name="context">The AspNetCore context.</param>
        /// <returns>The Nancy environment context.</returns>
        public static NancyContext GetNancyEnvironment(this HttpContext context) {
            return (context.Items.ContainsKey(NancyMiddleware.NancyEnvironmentKey) ?
                context.Items[NancyMiddleware.NancyEnvironmentKey] as NancyContext : null);
        }
    }
}