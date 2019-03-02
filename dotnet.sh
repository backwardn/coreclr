#!/usr/bin/env bash

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Running init-tools.sh"
source $working_tree_root/init-tools.sh

dotnet=$working_tree_root/.dotnet/dotnet

echo "Running: $dotnet $@"
$dotnet "$@"
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred in $dotnet $@. Check logs under $working_tree_root."
    exit 1
fi

echo "Command successfully completed."
exit 0
