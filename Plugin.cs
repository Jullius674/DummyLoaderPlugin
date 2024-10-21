using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using PhoneApp.Domain;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;

namespace DummyLoaderPlugin
{
    [Author(Name = "Nikita Osipov")]
    public class Plugin : IPluggable
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static Plugin()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            var employeesList = new List<EmployeesDTO>(args.Cast<EmployeesDTO>());
            logger.Info($"Инициализировано: {employeesList.Count}");
            LoadUsersFromApi(employeesList);
            logger.Info($"Итоговое количестко инициализированных: {employeesList.Count}");
            return employeesList.Cast<DataTransferObject>();
        }

        private void LoadUsersFromApi(List<EmployeesDTO> employeesList)
        {
            string apiUrl = "https://dummyjson.com/users";

            try
            {
                using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                {
                    HttpResponseMessage response = client.GetAsync(apiUrl).Result; // выполняем запрос HTTP
                    response.EnsureSuccessStatusCode();

                    string responseBody = response.Content.ReadAsStringAsync().Result; // читаем данные
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody); // десериализуем их

                    if (apiResponse != null && apiResponse.Users != null)
                    {
                        logger.Info($"Через API вернули {apiResponse.Users.Count} сотрудников.");
                    }

                    logger.Info($"Инициализировано: {employeesList.Count}");

                    foreach (var user in apiResponse.Users) // добавляем пользователей
                    {
                        string fullName = $"{user.FirstName} {(user.MaidenName ?? "")} {user.LastName}".Trim();

                        employeesList.Add(new EmployeesDTO
                        {
                            Name = fullName,
                            Phone = user.Phone
                        });

                        logger.Info($"добавлен сотрудник: {fullName}, мобильный телефон: {user.Phone}");
                        logger.Info($"Загружено сотрудников {employeesList.Count()}.");
                    }
                    logger.Info($"В итоге загружено: {employeesList.Count}");
                }
            }
            catch (HttpRequestException e) // ловим ошибки
            {
                logger.Error($"Ошибка {e.GetType()}: {e.Message}");
            }
        }

    }
    // классы 
    public class ApiResponse
    {
        public List<User> Users { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string MaidenName { get; set; } = "";
        public string LastName { get; set; }
        public string Phone { get; set; }
    }
}
