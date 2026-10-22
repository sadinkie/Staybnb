SELECT 'HostProperties' AS [Table], COUNT(*) AS [Rows] FROM HostProperties
UNION ALL SELECT 'PropertyImages', COUNT(*) FROM PropertyImages
UNION ALL SELECT 'CheckInProcesses', COUNT(*) FROM CheckInProcesses
UNION ALL SELECT 'HostApplications', COUNT(*) FROM HostApplications
UNION ALL SELECT 'AmenityHostProperty', COUNT(*) FROM AmenityHostProperty;