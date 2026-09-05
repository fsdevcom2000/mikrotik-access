[🇬🇧 English](README.md) | [🇷🇺 Русский](README.ru.md)

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

# License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

