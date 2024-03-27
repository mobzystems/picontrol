#!/bin/bash
echo "I am $(whoami) on $(hostname)"
echo "It is now $(date)"
echo -e "\nOpenVPN:"
sudo service openvpn status
echo -e "\nOpenVPN:"
sudo service rc-local status
echo -e "\nDone"
~/t.sh
~/t.sh
