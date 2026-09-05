# MikroTik Access Trigger

Small Windows CLI utility for triggering a MikroTik port-knocking access rule.

The utility sends two short TCP connection attempts to the configured MikroTik host. The first connection triggers the first firewall rule, then after a configurable delay the second connection triggers the next rule.

The TCP connections are not intended to be established. The initial connection attempt is enough to trigger the MikroTik rules.

## Usage

```text
mikrotik-access.exe [options]
```

Run without arguments to use the default configuration:

```text
mikrotik-access.exe
```

### Options

|Option|Description|Default|
|---|---|---|
|`--host <address>`|MikroTik IPv4 address or hostname|`192.168.88.1`|
|`--port1 <port>`|First trigger port|`54321`|
|`--port2 <port>`|Second trigger port|`12345`|
|`--delay <seconds>`|Delay between the two connection attempts|`3`|
|`--help`|Show help|-|

## Examples

Use default settings:

```text
mikrotik-access.exe
```

Specify the MikroTik address:

```text
mikrotik-access.exe --host 192.168.88.1
```

Use a hostname:

```text
mikrotik-access.exe --host router.example.com
```

Change the delay:

```text
mikrotik-access.exe --host router.example.com --delay 5
```

Specify all parameters:

```text
mikrotik-access.exe --host router.example.com --port1 54321 --port2 12345 --delay 5
```

## How it works

The utility performs the following sequence:

```text
TCP SYN -> port 54321
             |
             v
        MikroTik adds
        source IP to temp-list
             |
          delay
             |
             v
TCP SYN -> port 12345
             |
             v
        MikroTik adds
        source IP to access-list
             |
             v
       WinBox port 8291
       accepts the connection
```

The connection attempts are deliberately short. The utility only needs to send the initial TCP connection request; it does not need to establish a successful connection to either port.

## MikroTik configuration

The following RouterOS firewall rules implement the port-knocking sequence used by this utility.

The sequence is:

1. Only IP addresses in `access-list` are allowed to connect to WinBox on port `8291`.
    
2. A connection attempt to port `54321` adds the source IP to `temp-list` for 1 minute.
    
3. A connection attempt to port `12345` from an IP already present in `temp-list` adds that IP to `access-list`.
    

### 1. Allow WinBox only for `access-list`

```routeros
/ip firewall filter add chain=input protocol=tcp dst-port=8291 src-address-list=access-list action=accept comment="Allow WinBox from access-list"
```

You should also have a rule that drops other connections to the WinBox port:

```routeros
/ip firewall filter add chain=input protocol=tcp dst-port=8291 action=drop comment="Drop WinBox from unauthorized IPs"
```

Or use your common drop rule

### 2. First knock - port `54321`

Add the source IP to `temp-list` for one minute:

```routeros
/ip firewall filter add chain=input protocol=tcp connection-state=new dst-port=54321 in-interface-list=WAN action=add-src-to-address-list address-list=temp-list address-list-timeout=1m comment="Port knock 1"
```

### 3. Second knock - port `12345`

Only an IP already present in `temp-list` can complete the sequence:

```routeros
/ip firewall filter add chain=input protocol=tcp connection-state=new dst-port=12345 in-interface-list=WAN src-address-list=temp-list action=add-src-to-address-list address-list=access-list address-list-timeout=1h comment="Port knock 2"
```

The `access-list-timeout` value can be changed according to your requirements.

The two knock ports do not need to provide any actual service. Their purpose is only to trigger the firewall rules.

> Make sure the WinBox `accept` rule is placed before the corresponding `drop` rule in the firewall filter chain.

## Requirements

- Windows x64
    
- MikroTik RouterOS with a compatible firewall configuration
    

The published executable is self-contained and does not require a separate .NET runtime.

## License

MIT

# MikroTik Access Trigger

Небольшая CLI-утилита для Windows, предназначенная для запуска port-knocking правила на MikroTik.

Утилита отправляет две короткие TCP-попытки подключения на указанный MikroTik. После первой попытки выполняется заданная задержка, затем отправляется вторая попытка.

TCP-соединения не должны успешно устанавливаться. Для срабатывания правил MikroTik достаточно начальной попытки TCP-подключения.

## Использование

```text
mikrotik-access.exe [options]
```

Без параметров используются значения по умолчанию:

```text
mikrotik-access.exe
```

### Параметры

|Параметр|Описание|По умолчанию|
|---|---|---|
|`--host <address>`|IPv4-адрес или hostname MikroTik|`192.168.88.1`|
|`--port1 <port>`|Первый порт для запуска правила|`54321`|
|`--port2 <port>`|Второй порт для запуска правила|`12345`|
|`--delay <seconds>`|Задержка между двумя попытками|`3`|
|`--help`|Показать справку|-|

## Примеры

Использовать настройки по умолчанию:

```text
mikrotik-access.exe
```

Указать адрес MikroTik:

```text
mikrotik-access.exe --host 192.168.88.1
```

Использовать hostname:

```text
mikrotik-access.exe --host router.example.com
```

Изменить задержку:

```text
mikrotik-access.exe --host router.example.com --delay 5
```

Указать все параметры:

```text
mikrotik-access.exe --host router.example.com --port1 54321 --port2 12345 --delay 5
```

## Как это работает

Утилита выполняет следующую последовательность:

```text
TCP SYN -> порт 54321
             |
             v
        MikroTik добавляет
        IP-адрес в temp-list
             |
          задержка
             |
             v
TCP SYN -> порт 12345
             |
             v
        MikroTik добавляет
        IP-адрес в access-list
             |
             v
       WinBox порт 8291
       принимает подключение
```

Попытки подключения намеренно выполняются с коротким таймаутом. Утилите не требуется устанавливать полноценное TCP-соединение с этими портами - достаточно отправки начального запроса на подключение.

## Настройка MikroTik

Следующие правила RouterOS реализуют port-knocking последовательность, используемую этой утилитой.

Последовательность выглядит так:

1. Подключение к WinBox на порту `8291` разрешено только для IP-адресов из `access-list`.
    
2. Попытка подключения к порту `54321` добавляет IP-адрес источника в `temp-list` на 1 минуту.
    
3. Попытка подключения к порту `12345` от IP-адреса, который уже находится в `temp-list`, добавляет этот IP в `access-list`.
    

### 1. Разрешить WinBox только для `access-list`

```routeros
/ip firewall filter add chain=input protocol=tcp dst-port=8291 src-address-list=access-list action=accept comment="Allow WinBox from access-list"
```

Также необходимо правило, блокирующее остальные подключения к WinBox:

```routeros
/ip firewall filter add chain=input protocol=tcp dst-port=8291 action=drop comment="Drop WinBox from unauthorized IPs"
```

Или используйте общее DROP правило для всего остального

### 2. Первый Knock - порт `54321`

Добавляем IP-адрес источника в `temp-list` на одну минуту:

```routeros
/ip firewall filter add chain=input protocol=tcp connection-state=new dst-port=54321 in-interface-list=WAN action=add-src-to-address-list address-list=temp-list address-list-timeout=1m comment="Port knock 1"
```

Значение  `timeout` в `temp-list` можно изменить в соответствии с необходимым временем доступа, выставить чуть больше минуты если сеть не стабильна.

### 3. Второй Knock - порт `12345`

Только IP-адрес, который уже находится в `temp-list`, может завершить последовательность:

```routeros
/ip firewall filter add chain=input protocol=tcp connection-state=new dst-port=12345 in-interface-list=WAN src-address-list=temp-list action=add-src-to-address-list address-list=access-list address-list-timeout=1h comment="Port knock 2"
```

Значение `timeout` в `access-list`  можно изменить в соответствии с необходимым временем доступа.

Порты `54321` и `12345` не должны предоставлять какой-либо реальный сервис. Они используются только для срабатывания firewall-правил.

> Убедитесь, что правило `accept` для WinBox находится выше соответствующего правила `drop` в цепочке firewall filter.

## Лицензия

MIT
