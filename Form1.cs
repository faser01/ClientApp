using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp
{
   
    public partial class Form1 : Form
    {
        [Serializable]
        public class ClientSettings
        {
            public string ServerIp { get; set; }
            public int ServerPort { get; set; }
        }

        private TcpClient client;
        private NetworkStream stream;
        public Form1()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click_1(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Создаем клиент и подключаемся к серверу
                client = new TcpClient();

                try
                {
                    string serverIp = txtServerIp.Text;
                    int serverPort = int.Parse(txtServerPort.Text);

                    await client.ConnectAsync(serverIp, serverPort);
                    stream = client.GetStream();

                    await AddUser(username, password);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Введите имя пользователя и пароль.");
            }
        }

        private async Task AddUser(string username, string password)
        {
            try
            {
                // Отправляем запрос на сервер для регистрации нового пользователя
                string request = $"REGISTER|{username}|{password}";
                await SendMessageAsync(request);

                // Читаем ответ от сервера
                string response = await ReadMessageAsync<string>();

                if (response == "SUCCESS")
                {
                    MessageBox.Show("Пользователь успешно зарегистрирован.");
                }
                else
                {
                    MessageBox.Show("Ошибка регистрации пользователя.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации пользователя: {ex.Message}");
            }
            finally
            {
                // Закрываем соединение после завершения операции
                client.Close();
            }
        }



        private async void btnConnect_Click_1(object sender, EventArgs e)
        {
            try
            {
                string serverIp = txtServerIp.Text;
                int serverPort = int.Parse(txtServerPort.Text);

                // Создаем клиент и подключаемся к серверу
                client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);
                stream = client.GetStream();
                lblStatus.Text = "Подключено к серверу.";

                // Получаем список дел пользователя после успешной авторизации
                await GetTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        private async Task GetTasks()
        {
            // Отправляем запрос на получение списка дел
            await SendMessageAsync("GET_TASKS");

            // Читаем ответ от сервера
            List<string> tasks = await ReadMessageAsync<List<string>>();
            lstTasks.Items.Clear();
            lstTasks.Items.AddRange(tasks.ToArray());
        }

        private async Task AddTask()
        {
            string task = txtNewTask.Text;

            // Отправляем запрос на добавление нового дела
            await SendMessageAsync("ADD_TASK");
            await SendMessageAsync(task);

            // Обновляем список дел после добавления нового дела
            await GetTasks();
        }

        private async Task SendMessageAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async Task<T> ReadMessageAsync<T>()
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            byte[] messageBytes = new byte[bytesRead];
            Array.Copy(buffer, messageBytes, bytesRead);
            return Deserialize<T>(messageBytes);
        }

        private T Deserialize<T>(byte[] data)
        {
            using (var stream = new System.IO.MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }

        private async void btnAddTask_Click(object sender, EventArgs e)
        {
            await AddTask();
        }

       
    }
}
