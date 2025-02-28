using System.Data.SqlClient;
using Dapper;
using WeatherBot.model; 

namespace WeatherBot.Service
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public async Task<long> AddUserAsync(long telegramId, string userName, long chatId)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = @"
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = @UserId)
        BEGIN
            INSERT INTO Users (UserId, UserName, ChatId) 
            VALUES (@UserId, @UserName, @ChatId);
            SELECT @UserId;
        END
        ELSE
        BEGIN
            UPDATE Users 
            SET UserName = @UserName, ChatId = @ChatId 
            WHERE UserId = @UserId;
            SELECT UserId FROM Users WHERE UserId = @UserId;
        END";

            return await connection.ExecuteScalarAsync<long>(query, new { UserId = telegramId, UserName = userName, ChatId = chatId });
        }



        public async Task AddWeatherHistoryAsync(long userId, string city, string weatherInfo)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = "INSERT INTO WeatherHistory (UserId, City, Temperature) VALUES (@UserId, @City, @Temperature)";
            await connection.ExecuteAsync(query, new { UserId = userId, City = city, Temperature = weatherInfo });
        }
        
        public async Task<IEnumerable<WeatherHistory>> GetUserWeatherHistoryAsync(long userId)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = "SELECT * FROM WeatherHistory WHERE UserId = @UserId ORDER BY RequestTime DESC";
            return await connection.QueryAsync<WeatherHistory>(query, new { UserId = userId });
        }
        
        public async Task<User> GetUserAsync(long userId)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = "SELECT * FROM Users WHERE UserId = @UserId";
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { UserId = userId });
        }
        
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            string query = "SELECT * FROM Users";
            return await connection.QueryAsync<User>(query);
        }
    }
}
