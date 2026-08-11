$jsonPath = "NearU_Backend.postman_collection.json"
$jsonStr = Get-Content -Raw $jsonPath
$collection = $jsonStr | ConvertFrom-Json

$ridesFolder = @{
    name = "Rides"
    item = @(
        @{
            name = "Register Device Token"
            request = @{
                method = "POST"
                header = @()
                body = @{
                    mode = "raw"
                    raw = '{"token": "sample-fcm-device-token"}'
                    options = @{ raw = @{ language = "json" } }
                }
                url = @{
                    raw = "{{baseUrl}}/api/rides/device-token"
                    host = @("{{baseUrl}}")
                    path = @("api", "rides", "device-token")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Remove Device Token"
            request = @{
                method = "DELETE"
                header = @()
                body = @{
                    mode = "raw"
                    raw = '{"token": "sample-fcm-device-token"}'
                    options = @{ raw = @{ language = "json" } }
                }
                url = @{
                    raw = "{{baseUrl}}/api/rides/device-token"
                    host = @("{{baseUrl}}")
                    path = @("api", "rides", "device-token")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Get Active Ride"
            request = @{
                method = "GET"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/rides/active"
                    host = @("{{baseUrl}}")
                    path = @("api", "rides", "active")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        }
    )
}

$adminFolder = @{
    name = "Admin"
    item = @(
        @{
            name = "Get All Riders"
            request = @{
                method = "GET"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/admin/riders"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "riders")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Approve Rider"
            request = @{
                method = "PUT"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/admin/riders/REPLACE_WITH_RIDER_ID/approve"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "riders", "REPLACE_WITH_RIDER_ID", "approve")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Reject Rider"
            request = @{
                method = "PUT"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/admin/riders/REPLACE_WITH_RIDER_ID/reject"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "riders", "REPLACE_WITH_RIDER_ID", "reject")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Suspend Rider"
            request = @{
                method = "PUT"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/admin/riders/REPLACE_WITH_RIDER_ID/suspend"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "riders", "REPLACE_WITH_RIDER_ID", "suspend")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Set Rider Tier"
            request = @{
                method = "PUT"
                header = @()
                body = @{
                    mode = "raw"
                    raw = '{"tier": "Premium"}'
                    options = @{ raw = @{ language = "json" } }
                }
                url = @{
                    raw = "{{baseUrl}}/api/admin/riders/REPLACE_WITH_RIDER_ID/tier"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "riders", "REPLACE_WITH_RIDER_ID", "tier")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        },
        @{
            name = "Get Platform Stats"
            request = @{
                method = "GET"
                header = @()
                url = @{
                    raw = "{{baseUrl}}/api/admin/stats"
                    host = @("{{baseUrl}}")
                    path = @("api", "admin", "stats")
                }
                auth = @{
                    type = "bearer"
                    bearer = @( @{ key = "token"; value = "{{accessToken}}"; type = "string" } )
                }
            }
            response = @()
        }
    )
}

# Add new folders if they don't exist
$items = $collection.item
$hasRides = $false
$hasAdmin = $false

foreach ($item in $items) {
    if ($item.name -eq "Rides") { $hasRides = $true }
    if ($item.name -eq "Admin") { $hasAdmin = $true }
}

if (-not $hasRides) {
    $collection.item += $ridesFolder
}
if (-not $hasAdmin) {
    $collection.item += $adminFolder
}

$collection | ConvertTo-Json -Depth 10 | Set-Content $jsonPath -Encoding utf8
Write-Output "Postman collection updated successfully."
