using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http;


namespace WebAppExperimental266.Models.ResultObjects
{

    public class CustomResultFactory
    {

        /// <summary>
        /// Creates an <see cref="UnprocessableEntityResult"/> that produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
        public virtual UnprocessableEntityResult UnprocessableEntity()
        {
            return new UnprocessableEntityResult();
        }


    }




    /// <summary>
    /// Represents an <see cref="StatusCodeResult"/> that when
    /// executed will produce a Not Found (422) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class UnprocessableEntityResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status422UnprocessableEntity;

        /// <summary>
        /// Creates a new <see cref="UnprocessableEntity"/> instance.
        /// </summary>
        public UnprocessableEntityResult() : base(DefaultStatusCode)
        {
        }
    }


}
