# WSL Cheatsheet

## List distributions

`wsl -v -l`

## Start or enter distribution

`wsl -d <instance-name>
`wsl -d Ollama-Jailed`

## WSL IP: 172.24.209.117

Currently changes on restart

## Port Proxy

netsh interface portproxy add v4tov4 listenport=11434 listenaddress=0.0.0.0 connectport=11434 connectaddress=172.24.209.117

## Windows Firewall

New-NetFirewallRule -DisplayName "Ollama LAN Access" -Direction Inbound -LocalPort 11434 -Protocol TCP -Action Allow

## Script to Setup Proxy and Firewall

TBD - intended to be run when WSL starts so it's available automatically
