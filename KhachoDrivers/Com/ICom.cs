using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace KhachoDrivers
{
	/// <summary>
	/// Интерфейс COM-порта.
	/// </summary>
	/// <remarks>
	/// Используется для тестирования приложения без участия реального COM-порта и ответного устройства.
	/// </remarks>
	public interface ICom : IDisposable
	{
		#region {PROPERTIES}

		/// <summary>
		/// Таймаут ожидания чтения [мс].
		/// </summary>
		int ReadTimeOut { get; set; }

		/// <summary>
		/// Таймаут ожидания записи [мс].
		/// </summary>
		int WriteTimeOut { get; set; }

		/// <summary>
		/// Признак успешного открытия COM-порта.
		/// </summary>
		bool IsOpen { get; }

		/// <summary>
		/// Связанные с текущим объектом данные.
		/// </summary>
		object Tag { get; set; }

		#endregion


		#region {EVENTS}

		/// <summary>
		/// Происходит, если были приняты данный по COM-порту.
		/// </summary>
		event SerialDataReceivedEventHandler DataReceived;

		/// <summary>
		/// Происходит после изменения параметра IsOpen объекта класса COM.
		/// </summary>
		event EventHandler OpenStateChanged;

		#endregion


		#region {METHODS}

		/// <summary>
		/// Проводит смену номера COM-порта и его открытие.
		/// </summary>
		/// <param name="newNum">Новый номер.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		bool ChangeComNum(int newNum);

		/// <summary>
		/// Проводит смену скорости передачи по COM-порту.
		/// </summary>
		/// <param name="newBaudRate">Новое значение скорости передачи [бод].</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		bool ChangeComBaudRate(int newBaudRate);

		/// <summary>
		/// Проводит смену режима проверки четности COM-порта.
		/// </summary>
		/// <param name="newParity">Новый режим проверки четности.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		bool ChangeComParity(Parity newParity);

		/// <summary>
		/// Проводит смену значения количества битов данных COM-порта.
		/// </summary>
		/// <param name="newDataBits">Новое значение количества битов данных.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		bool ChangeComDataBits(int newDataBits);

		/// <summary>
		/// Проводит смену значения количества стоповых битов COM-порта.
		/// </summary>
		/// <param name="newStopBits">Новое значение количества стоповых битов.</param>
		/// <returns>true - смена произошла успешно; false - смены не произошло.</returns>
		bool ChangeComStopBits(StopBits newStopBits);

		/// <summary>
		/// Закрывает соединение по COM-порту сохраняя объект соединение в целости.
		/// </summary>
		void Close();

		/// <summary>
		/// Освобождает все ресурсы, используемые объектом LenzeStendApplication.Drivers.COM.
		/// </summary>
		void Dispose();

		/// <summary>
		/// Возвращает текущий номер COM-порта.
		/// </summary>
		/// <returns>Номер COM-порта.</returns>
		int GetNum();

		/// <summary>
		/// Возвращает текущую скорость соединения.
		/// </summary>
		/// <returns>Скорость соединения.</returns>
		int GetBaudRate();

		/// <summary>
		/// Возвращает текущее значение режима проверки четности.
		/// </summary>
		/// <returns>Значение режима проверки четности.</returns>
		Parity GetParity();

		/// <summary>
		/// Возвращает текущее значение количества бит данных.
		/// </summary>
		/// <returns>Текущее значение количества бит данных.</returns>
		int GetDataBits();

		/// <summary>
		/// Возвращает текущее значение количетства стоповых бит.
		/// </summary>
		/// <returns>Текущее значение количества стоповых бит.</returns>
		StopBits GetStopBits();

		/// <summary>
		/// Возвращает считанные из COM-порта данные.
		/// </summary>
		/// <returns>Считанные из COM-порта данные.</returns>
		List<byte> GetReadedBytes();

		/// <summary>
		/// Восстанавливает соединение по COM-порту.
		/// </summary>
		void Resume();

		/// <summary>
		/// Перезагружает соединение по COM-порту.
		/// </summary>
		void ResetConnection();

		/// <summary>
		/// Посылка команды по COM-порту.
		/// </summary>
		/// <param name="message">Отправляемое значение.</param>
		/// <returns>Результат отправки посылки.</returns>
		bool SendMessage(byte[] message);

		#endregion
	}
}
