using System.Threading.Tasks;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Strategy Interface - Defines the contract for all AI model strategies
    /// </summary>
    public interface IAiModelStrategy
    {
        /// <summary>
        /// Gets the model identifier used by OpenRouter
        /// </summary>
        string ModelId { get; }

        /// <summary>
        /// Gets the display name of the model
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Indicates if this model supports vision/image input
        /// </summary>
        bool SupportsVision { get; }

        /// <summary>
        /// Generates a response from the AI model
        /// </summary>
        /// <param name="prompt">The user's input prompt</param>
        /// <param name="systemInstructions">Optional system instructions to guide the model</param>
        /// <param name="temperature">Controls randomness (0.0 to 2.0)</param>
        /// <param name="maxTokens">Maximum tokens to generate</param>
        /// <returns>The AI-generated response text</returns>
        Task<string> GenerateResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000
        );

        /// <summary>
        /// Generates a response with image input support
        /// </summary>
        /// <param name="prompt">The user's text prompt</param>
        /// <param name="imageBase64List">List of base64-encoded images</param>
        /// <param name="systemInstructions">Optional system instructions</param>
        /// <param name="temperature">Controls randomness</param>
        /// <param name="maxTokens">Maximum tokens to generate</param>
        /// <returns>The AI-generated response text</returns>
        Task<string> GenerateResponseWithImagesAsync(
            string prompt,
            List<string> imageBase64List,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000
        );

        /// <summary>
        /// Generates a streaming response from the AI model
        /// </summary>
        Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000
        );

        /// <summary>
        /// Generates a streaming response with image input support
        /// </summary>
        Task<IAsyncEnumerable<string>> GenerateStreamingResponseWithImagesAsync(
            string prompt,
            List<string> imageBase64List,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000
        );
    }
}
