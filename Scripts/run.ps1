# Kill existing process
taskkill /F /IM monitor-controller.exe 2>$null

# Run the application and auto open settings form
dotnet run --settings