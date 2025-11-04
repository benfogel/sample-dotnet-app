#!/bin/bash

# Start the dotnet application in the background
dotnet app.dll &

# (
# echo "--- Running FIO with $FIO_SIZE  ---"
# fio --name=standard-test --filename=/app/logs/testfile.dat --ioengine=libaio --size=${FIO_SIZE:-10G} --verify=0 --randrepeat=0 --bs=4k --direct=1 --rw=randrw --rate=51200k,51200k --time_based --runtime=7d --numjobs=1 --iodepth=32 --output=/app/logs/fio-results.log 
# ) &

(
  while true; do
    echo "--- Writing to /app/logs/logfile continuously ---"
    head -c 150G </dev/urandom >/app/logs/logfile
  done
) &


# Give processes a moment to start
sleep 2

(
  echo "--- Running iostat continuously ---"
  disk_device=$(df /app/logs | awk 'NR==2 {print $1}')
  iostat -dx $(basename "$disk_device") 10
) &

# (
#   echo "--- Running pidstat for fio continuously ---"
#   fio_pids=$(pgrep -x fio | paste -sd ',')
#   if [[ -n "$fio_pids" ]]; then
#     pidstat -d -p "$fio_pids" 10
#   else
#     echo "fio process not found."
#   fi
# ) &

# (
#   echo "--- Running pidstat for dotnet continuously ---"
#   dotnet_pids=$(pgrep -x dotnet | paste -sd ',')
#   if [[ -n "$dotnet_pids" ]]; then
#     pidstat -d -p "$dotnet_pids" 10
#   else
#     echo "dotnet process not found."
#   fi
# ) &


# Wait for any process to exit
wait -n

# Exit with status of process that exited first
exit $?
