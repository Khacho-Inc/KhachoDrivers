using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KhachoUtils;

namespace KhachoDrivers
{
	/// <summary>
	/// Драйвер COM-порта.
	/// </summary>
	public class COM : IDisposable
	{
		#region {CONSTANTS}

		/// <summary>
		/// Общепринятые стандарты скорости передачи по UART [бит/с].
		/// </summary>
		public static readonly int[] BaudRates = { 921600, 460800, 230400, 115200, 57600, 38400, 19200, 9600, 4800, 2400, 1200, 600, 300 };

		#endregion


		#region {PROPERTIES}

		/// <summary>
		/// Таймаут ожидания чтения [мс].
		/// </summary>
		public int ReadTimeOut
		{
			get { return readTimeOut; }
			set { readTimeOut = value; }
		}
		private int readTimeOut = SerialPort.InfiniteTimeout;

		/// <summary>
		/// Таймаут ожидания записи [мс].
		/// </summary>
		public int WriteTimeOut
		{
			get { return writeTimeOut; }
			set { writeTimeOut = value; }
		}
		private int writeTimeOut = 1000;

		/// <summary>
		/// Признак успешного открытия COM-порта.
		/// </summary>
		public bool IsOpen
		{
			get { return isOpen; }
			private set
			{
				// признак изменения состояния
				var changed = (isOpen != value);
				// устанавливаем значение
				isOpen = value;
				// провоцируем событие
				if (changed == true && OpenStateChanged != null) OpenStateChanged(this, new EventArgs());
			}
		}
		private bool isOpen;

		/// <summary>
		/// Связанные с текущим объектом данные.
		/// </summary>
		public object Tag { get; set; }

		#endregion


		#region {MEMBERS}

		/// <summary>
		/// COM-порт.
		/// </summary>
		SerialPort com;

		/// <summary>
		/// Номер COM-порта.
		/// </summary>
		int num = -1;

		/// <summary>
		/// Частота соединенмя [бод].
		/// </summary>
		int baudRate = BaudRates[3];

		/// <summary>
		/// Проверка четности.
		/// </summary>
		Parity parity = Parity.None;

		/// <summary>
		/// Количество бит данных.
		/// </summary>
		int dataBits = 8;

		/// <summary>
		/// Количество стоповых битов.
		/// </summary>
		StopBits stopBits = StopBits.One;

		/// <summary>
		/// Считанные из порта байты.
		/// </summary>
		volatile List<byte> readedBytes = new List<byte>();

		/// <summary>
		/// Лог сообщений.
		/// </summary>
		LogWPF messageLog;

		#endregion


		#region {EVENTS}

		/// <summary>
		/// Происходит, если были приняты данный по COM-порту.
		/// </summary>
		public event SerialDataReceivedEventHandler DataReceived;

		/// <summary>
		/// Происходит после изменения параметра IsOpen объекта класса COM.
		/// </summary>
		public event EventHandler OpenStateChanged;

		#endregion


		#region {CONSTRUCTOR}

		/// <summary>
		/// Инициализирует новый экземпляр класса Drivers.COM с применением параметров по-умолчанию.
		/// </summary>
		public COM() { }

		/// <summary>
		/// Инициализирует новый экземпляр класса Drivers.COM с применением параметров по-умолчанию и логом сообщений.
		/// </summary>
		/// <param name="log">Лог сообщений.</param>
		public COM(LogWPF log)
		{
			messageLog = log;
		}

		/// <summary>
		/// Инициализирует новый экземпляр класса Drivers.COM с применением заданных параметров.
		/// </summary>
		/// <param name="num">Номер COM-порта.</param>
		/// <param name="baudRate">Скорость соединения.</param>
		/// <param name="parity">Проверка четности.</param>
		/// <param name="dataBits">Количество бит данных.</param>
		/// <param name="stopBits">Количество стоповых битов.</param>
		public COM(int num, int baudRate, Parity parity, int dataBits, StopBits stopBits)
		{
			this.num = num;
			this.baudRate = baudRate;
			this.parity = parity;
			this.dataBits = dataBits;
			this.stopBits = stopBits;
		}

		/// <summary>
		/// Инициализирует новый экземпляр класса Drivers.COM с применением заданных параметров и логом сообщений.
		/// </summary>
		/// <param name="num">Номер COM-порта.</param>
		/// <param name="baudRate">Скорость соединения.</param>
		/// <param name="parity">Проверка четности.</param>
		/// <param name="dataBits">Количество бит данных.</param>
		/// <param name="stopBits">Количество стоповых битов.</param>
		/// <param name="log">Лог сообщений.</param>
		public COM(int num, int baudRate, Parity parity, int dataBits, StopBits stopBits, LogWPF log)
		{
			this.num = num;
			this.baudRate = baudRate;
			this.parity = parity;
			this.dataBits = dataBits;
			this.stopBits = stopBits;
			messageLog = log;
		}

		/// <summary>
		/// Деструктор класса COM.
		/// </summary>
		~COM()
		{
			// освобождаем ресурсы, занимаемые COM-портом
			if (com != null)
			{
				com.Close();
				com.DataReceived -= com_DataReceived;
				com.Dispose();
			}
		}

		#endregion


		#region {PUBLIC_METHODS}

		/// <summary>
		/// Проводит смену номера COM-порта и его открытие.
		/// </summary>
		/// <param name="newNum">Новый номер.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		public bool ChangeComNum(int newNum)
		{
			// результат смены
			var res = true;
			// запоминаем старый номер порта
			var oldNum = num;
			// запоминаем признак успешного открытия порта до его перезагрузки
			var comIsOpenBeforeReload = IsOpen;
			// формируем новый номер
			num = newNum;

			// проводим перезагрузку порта
			openCOM();

			// порт не был открыт && до попытки перезагрузить порт он был открыт
			if (IsOpen == false && comIsOpenBeforeReload == true)
			// возвращаем порт к прежнему состоянию
			{
				// возвращаем старый номер порта
				num = oldNum;
				// перезагружаем порт
				openCOM();
				// меняет результат на отрицательный
				res = false;
			}

			// проводим индикацию текущих параметров COM-порта
			num = (IsOpen) ? num : -1;

			return res;
		}

		/// <summary>
		/// Проводит смену скорости передачи по COM-порту.
		/// </summary>
		/// <param name="newBaudRate">Новое значение скорости передачи [бод].</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		public bool ChangeComBaudRate(int newBaudRate)
		{
			// результат смены
			var res = true;
			// запоминаем старое значение скорости
			var oldBaudRate = baudRate;
			// запоминаем признак успешного открытия порта до его перезагрузки
			var comIsOpenBeforeReload = IsOpen;
			// формируем новое значение скорости
			baudRate = newBaudRate;

			// если порт был открыт, то пытаемся его перезагрузить
			if (IsOpen)
			{
				// проводим перезагрузку порта
				openCOM();

				// порт не был открыт && до попытки перезагрузить порт он был открыт
				if (IsOpen == false && comIsOpenBeforeReload == true)
				// возвращаем порт к прежнему состоянию
				{
					// возвращаем старое значение скорости
					baudRate = oldBaudRate;
					// перезагружаем порт
					openCOM();
					// меняет результат на отрицательный
					res = false;
				}
			}

			// проводим индикацию текущих параметров COM-порта
			if (com != null) baudRate = com.BaudRate;

			return res;
		}

		/// <summary>
		/// Проводит смену режима проверки четности COM-порта.
		/// </summary>
		/// <param name="newParity">Новый режим проверки четности.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		public bool ChangeComParity(Parity newParity)
		{
			// результат смены
			var res = true;
			// запоминаем старое значение количества битов данных
			var oldParity = parity;
			// запоминаем признак успешного открытия порта до его перезагрузки
			var comIsOpenBeforeReload = IsOpen;
			// формируем новое значение количества битов данных
			parity = newParity;

			// если порт был открыт, то пытаемся его перезагрузить
			if (IsOpen)
			{
				// проводим перезагрузку порта
				openCOM();

				// порт не был открыт && до попытки перезагрузить порт он был открыт
				if (IsOpen == false && comIsOpenBeforeReload == true)
				// возвращаем порт к прежнему состоянию
				{
					// возвращаем старое значение количества битов данных
					parity = oldParity;
					// перезагружаем порт
					openCOM();
					// меняет результат на отрицательный
					res = false;
				}
			}

			// проводим индикацию текущих параметров COM-порта
			if (com != null) parity = com.Parity;

			return res;
		}

		/// <summary>
		/// Проводит смену значения количества битов данных COM-порта.
		/// </summary>
		/// <param name="newDataBits">Новое значение количества битов данных.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		public bool ChangeComDataBits(int newDataBits)
		{
			// результат смены
			var res = true;
			// запоминаем старое значение количества битов данных
			var oldDataBits = dataBits;
			// запоминаем признак успешного открытия порта до его перезагрузки
			var comIsOpenBeforeReload = IsOpen;
			// формируем новое значение количества битов данных
			dataBits = newDataBits;

			// если порт был открыт, то пытаемся его перезагрузить
			if (IsOpen)
			{
				// проводим перезагрузку порта
				openCOM();

				// порт не был открыт && до попытки перезагрузить порт он был открыт
				if (IsOpen == false && comIsOpenBeforeReload == true)
				// возвращаем порт к прежнему состоянию
				{
					// возвращаем старое значение количества битов данных
					dataBits = oldDataBits;
					// перезагружаем порт
					openCOM();
					// меняет результат на отрицательный
					res = false;
				}
			}

			// проводим индикацию текущих параметров COM-порта
			if (com != null) dataBits = com.DataBits;

			return res;
		}

		/// <summary>
		/// Проводит смену значения количества стоповых битов COM-порта.
		/// </summary>
		/// <param name="newStopBits">Новое значение количества стоповых битов.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		public bool ChangeComStopBits(StopBits newStopBits)
		{
			// результат смены
			var res = true;
			// запоминаем старое значение количества стоповых битов
			var oldStopBits = stopBits;
			// запоминаем признак успешного открытия порта до его перезагрузки
			var comIsOpenBeforeReload = IsOpen;
			// формируем новое значение количества стоповых битов
			stopBits = newStopBits;

			// если порт был открыт, то пытаемся его перезагрузить
			if (IsOpen)
			{
				// проводим перезагрузку порта
				openCOM();

				// порт не был открыт && до попытки перезагрузить порт он был открыт
				if (IsOpen == false && comIsOpenBeforeReload == true)
				// возвращаем порт к прежнему состоянию
				{
					// возвращаем старое значение количества стоповых битов
					stopBits = oldStopBits;
					// перезагружаем порт
					openCOM();
					// меняет результат на отрицательный
					res = false;
				}
			}

			// проводим индикацию текущих параметров COM-порта
			if (com != null) stopBits = com.StopBits;

			return res;
		}

		/// <summary>
		/// Закрывает соединение по COM-порту сохраняя объект соединение в целости.
		/// </summary>
		public void Close()
		{
			if (com != null)
			{
				// закрываем соединение
				com.Close();
				// выставляем признак наличия подключения
				this.IsOpen = com.IsOpen;
			}
		}

		/// <summary>
		/// Освобождает все ресурсы, используемые объектом LenzeStendApplication.Drivers.COM.
		/// </summary>
		public void Dispose()
		{
			com.Close();
// TODO: генерируем событие изменения состояния порта
			com.Dispose();
		}

		/// <summary>
		/// Возвращает текущий номер COM-порта.
		/// </summary>
		/// <returns>Номер COM-порта.</returns>
		public int GetNum()
		{
			return num;
		}

		/// <summary>
		/// Возвращает текущую скорость соединения.
		/// </summary>
		/// <returns>Скорость соединения.</returns>
		public int GetBaudRate()
		{
			return baudRate;
		}

		/// <summary>
		/// Возвращает текущее значение режима проверки четности.
		/// </summary>
		/// <returns>Значение режима проверки четности.</returns>
		public Parity GetParity()
		{
			return parity;
		}

		/// <summary>
		/// Возвращает текущее значение количества бит данных.
		/// </summary>
		/// <returns>Текущее значение количества бит данных.</returns>
		public int GetDataBits()
		{
			return dataBits;
		}

		/// <summary>
		/// Возвращает текущее значение количетства стоповых бит.
		/// </summary>
		/// <returns>Текущее значение количества стоповых бит.</returns>
		public StopBits GetStopBits()
		{
			return stopBits;
		}

		/// <summary>
		/// Возвращает считанные из COM-порта данные.
		/// </summary>
		/// <returns>Считанные из COM-порта данные.</returns>
		public List<byte> GetReadedBytes()
		{
			// создаем временный буфер, в который будут перезаписаны данные
			var buff = new byte[readedBytes.Count()];
			// переносим передаваемые данные во вновь созданный буфер
			readedBytes.CopyTo(0, buff, 0, buff.Length);
			// удаляем оригинал скопированных данных
			readedBytes.RemoveRange(0, buff.Length);
			// возвращаем извлеченные данные
			return buff.ToList();
		}

		/// <summary>
		/// Восстанавливает соединение по COM-порту.
		/// </summary>
		public void Resume()
		{
			if (com != null)
			{
				// открываем соединение
				com.Open();
				// выставляем признак наличия подключения
				this.IsOpen = com.IsOpen;
			}
		}

		/// <summary>
		/// Перезагружает соединение по COM-порту.
		/// </summary>
		public void ResetConnection()
		{
			// перезагружаем соединение, если оно ранее было установлено
			if (IsOpen == true) openCOM();
		}

		/// <summary>
		/// Посылка команды по COM-порту.
		/// </summary>
		/// <param name="message">Отправляемое значение.</param>
		/// <returns>Результат отправки посылки.</returns>
		public bool SendMessage(byte[] message)
		{
			// результат отправки посылки
			var res = false;

			// пытаемся отправить, если порт был открыт
			if (IsOpen)
			{
				try
				{
					// отправка
					com.Write(message, 0, message.Length);
					// результат отправки меняем на положительный
					res = true;
					// вносим запись в лог сообщений
					if (messageLog != null)
					{
						var record = new StringBuilder();
						record.AppendFormat(" --> {0,20} :", ASCIIEncoding.ASCII.GetString(message));
						foreach (var symb in message) record.AppendFormat(" {0:X2}", symb);
						messageLog.LogRecord(record.ToString());
					}
				}
				catch { }
			}

			// возвращаем результат отправки
			return res;
		}

		#endregion


		#region {PRIVATE_METHODS}

		/// <summary>
		/// Открывает соединение по COM-порту.
		/// </summary>
		private void openCOM()
		{
			// если порт уже был создан ранее
			if (com != null)
			{
				// закрываем соединение
				com.Close();
				// отписываем порт от события принятия данных
				com.DataReceived -= com_DataReceived;
				// очищаем буферы
				try
				{
					com.DiscardInBuffer();
					com.DiscardOutBuffer();
				}
				catch { }
				// освобождаем ресурсы, связанные с портом
				com.Dispose();
			}

			// очищаем приемный буфер
			readedBytes.Clear();

			try
			{
				// создаем новый экземпляр порта
				com = new SerialPort(string.Concat("COM", num), baudRate, parity, dataBits, stopBits);
				com.ReadTimeout = ReadTimeOut;
				com.WriteTimeout = WriteTimeOut;
				// пытаемся установить соединение
				com.Open();
				// подписываем порт на событие принятия данных
				com.DataReceived += com_DataReceived;
			}
			catch (Exception excp)
			{
				// сообщаем об ошибке открытия порта
				throw new Exception(string.Format("Ошибка открытия COM-порта:\n\n{0}", excp.Message));
			}

			// выставляем статус порта
			IsOpen = com.IsOpen;
		}

		#endregion


		#region {EVENT_METHODS}

		private void com_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			// сохраняем принятые данные
				var buff = new byte[com.BytesToRead];
				com.Read(buff, 0, buff.Length);
				readedBytes.AddRange(buff);
			// генерируем событие принятия данных
				if (DataReceived != null) DataReceived(this, e);
			// вносим запись в лог сообщений
			if (messageLog != null)
			{
				var record = new StringBuilder();
				record.AppendFormat(" <-- {0,20} :", ASCIIEncoding.ASCII.GetString(buff));
				foreach(var item in buff) record.AppendFormat(" {0:X2}", item);
				messageLog.LogRecord(record.ToString());
			}
		}

		#endregion
	}
}
