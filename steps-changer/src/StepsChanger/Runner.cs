using System;
using System.Threading.Tasks;
using StepsChanger.Services;

namespace StepsChanger
{
    /// <summary>
    /// Main runner class for steps modification
    /// </summary>
    public class Runner
    {
        /// <summary>
        /// Change steps using Zepp account credentials
        /// </summary>
        /// <param name="account">Zepp account (email or phone)</param>
        /// <param name="password">Zepp password</param>
        /// <param name="steps">Number of steps to set</param>
        /// <returns>Success message or error message</returns>
        public static async Task<string> ChangeStepsAsync(string account, string password, string steps)
        {
            try
            {
                // Convert steps to int
                if (!int.TryParse(steps, out int stepsInt))
                {
                    return "步数格式错误，请输入数字";
                }

                if (stepsInt < 0 || stepsInt > 100000)
                {
                    return "步数范围应在 0-100000 之间";
                }

                // Step 1: Get access token
                using var authService = new ZeppAuthService();
                var (token, error) = await authService.GetAccessTokenAsync(account, password);
                
                if (string.IsNullOrEmpty(token))
                {
                    return $"登录失败: {error ?? "未知错误"}";
                }

                // Step 2: Get user ID
                using var stepsService = new StepsService();
                var (success, userId, userError) = await stepsService.GetUserInfoAsync(token);
                
                if (!success || string.IsNullOrEmpty(userId))
                {
                    return $"获取用户信息失败: {userError ?? "未知错误"}";
                }

                // Step 3: Submit steps
                var (submitSuccess, submitError) = await stepsService.SubmitStepsAsync(token, userId, stepsInt);
                
                if (submitSuccess)
                {
                    return $"成功设置步数为 {stepsInt} 步";
                }
                else
                {
                    return $"设置步数失败: {submitError ?? "未知错误"}";
                }
            }
            catch (Exception ex)
            {
                return $"发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// Change steps using existing app token (for Quicker integration)
        /// </summary>
        /// <param name="appToken">Zepp app token</param>
        /// <param name="steps">Number of steps to set</param>
        /// <returns>Success message or error message</returns>
        public static async Task<string> ChangeStepsWithTokenAsync(string appToken, string steps)
        {
            try
            {
                // Convert steps to int
                if (!int.TryParse(steps, out int stepsInt))
                {
                    return "步数格式错误，请输入数字";
                }

                if (stepsInt < 0 || stepsInt > 100000)
                {
                    return "步数范围应在 0-100000 之间";
                }

                // Get user ID
                using var stepsService = new StepsService();
                var userInfoResult = await stepsService.GetUserInfoAsync(appToken);
                
                if (!userInfoResult.success || string.IsNullOrEmpty(userInfoResult.userId))
                {
                    return $"获取用户信息失败: {userInfoResult.error ?? "未知错误"}";
                }

                // Submit steps
                var submitResult = await stepsService.SubmitStepsAsync(appToken, userInfoResult.userId, stepsInt);
                
                if (submitResult.success)
                {
                    return $"成功设置步数为 {stepsInt} 步";
                }
                else
                {
                    return $"设置步数失败: {submitResult.error ?? "未知错误"}";
                }
            }
            catch (Exception ex)
            {
                return $"发生错误: {ex.Message}";
            }
        }
    }
}
