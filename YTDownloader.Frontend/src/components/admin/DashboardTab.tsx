'use client'

import React, { useState, useEffect } from 'react'
import StatsCard from './StatsCard'
import { DownloadRecord } from '@/app/admin/page'

interface DashboardTabProps {
    downloads: DownloadRecord[]
}

interface SystemVersions {
    ytdlp: string
    ffmpeg: string
    service: string
    timestamp: string
}

interface MinioStats {
    totalFiles: number
    totalSize: number
    totalSizeFormatted: string
    bucketName: string
    timestamp: string
}

interface User {
    id: string
    email: string
    firstName: string
    lastName: string
    roles: string[]
}

export default function DashboardTab({ downloads }: DashboardTabProps) {
    const [versions, setVersions] = useState<SystemVersions | null>(null)
    const [minioStats, setMinioStats] = useState<MinioStats | null>(null)
    const [users, setUsers] = useState<User[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        fetchDashboardData()
    }, [])

    const getAuthHeaders = () => {
        const token = localStorage.getItem('accessToken')
        if (!token) throw new Error('Authentication required')
        return {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
        }
    }

    const handleAuthError = (status: number): string | null => {
        if (status === 401) {
            localStorage.removeItem('accessToken')
            return 'Session expired. Please login again.'
        }
        if (status === 403) {
            return 'You do not have permission to access this data.'
        }
        return null
    }

    const fetchDashboardData = async () => {
        try {
            console.log('[DashboardTab] Fetching dashboard data...')
            setLoading(true)
            setError(null)

            const [versionsRes, minioRes, usersRes] = await Promise.all([
                fetch('/api/admin/system/versions', { method: 'GET', headers: getAuthHeaders() }),
                fetch('/api/admin/minio/stats', { method: 'GET', headers: getAuthHeaders() }),
                fetch('/api/admin/users', { method: 'GET', headers: getAuthHeaders() }),
            ])

            // Check for auth errors on any response
            const responses = [
                { res: versionsRes, name: 'versions' },
                { res: minioRes, name: 'minio' },
                { res: usersRes, name: 'users' },
            ]

            for (const { res, name } of responses) {
                const authError = handleAuthError(res.status)
                if (authError) {
                    console.error(`[DashboardTab] ${name} auth error:`, res.status)
                    setError(authError)
                    setLoading(false)
                    return
                }
                if (!res.ok) {
                    throw new Error(`Failed to fetch ${name}`)
                }
            }

            const [versionsData, minioData, usersData] = await Promise.all([
                versionsRes.json(),
                minioRes.json(),
                usersRes.json(),
            ])

            setVersions(versionsData)
            setMinioStats(minioData)
            setUsers(Array.isArray(usersData) ? usersData : [])
            console.log('[DashboardTab] ✓ Data loaded')
        } catch (err) {
            console.error('[DashboardTab] Error:', err)
            setError(err instanceof Error ? err.message : 'Failed to load dashboard data')
        } finally {
            setLoading(false)
        }
    }

    const stats = [
        { label: 'Total Users', value: users.length.toLocaleString(), icon: '👥', color: 'from-purple-500 to-pink-500' },
        { label: 'Storage Used', value: minioStats?.totalSizeFormatted || '—', icon: '💾', color: 'from-orange-500 to-red-500' },
        { label: 'Files in Storage', value: minioStats?.totalFiles.toLocaleString() || '—', icon: '📦', color: 'from-green-500 to-emerald-500' },
    ]

    return (
        <div className="space-y-6">
            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {stats.map((stat, i) => (
                    <StatsCard key={i} {...stat} />
                ))}
            </div>

            {/* System Health */}
            <div className="p-6 rounded-2xl bg-white/5 border border-white/10">
                <h2 className="text-xl font-bold mb-4">System Health</h2>
                {error ? (
                    <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg">
                        <p className="text-red-400 text-sm">{error}</p>
                        <button
                            onClick={fetchDashboardData}
                            className="mt-2 px-3 py-1 text-xs bg-red-500/20 rounded hover:bg-red-500/30 transition-all"
                        >
                            Retry
                        </button>
                    </div>
                ) : loading ? (
                    <p className="text-gray-400 text-sm">Loading system info...</p>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div className="p-3 bg-white/5 rounded-lg">
                            <p className="text-xs text-gray-400">YT-DLP Version</p>
                            <p className="text-sm font-semibold text-cyan-400">{versions?.ytdlp || '—'}</p>
                        </div>
                        <div className="p-3 bg-white/5 rounded-lg">
                            <p className="text-xs text-gray-400">FFmpeg Version</p>
                            <p className="text-sm font-semibold text-cyan-400">{versions?.ffmpeg || '—'}</p>
                        </div>
                        <div className="p-3 bg-white/5 rounded-lg">
                            <p className="text-xs text-gray-400">Status</p>
                            <p className="text-sm font-semibold text-green-400">✓ Online</p>
                        </div>
                    </div>
                )}
            </div>

            {/* Recent Downloads */}
            <div className="p-6 rounded-2xl bg-white/5 border border-white/10">
                <h2 className="text-xl font-bold mb-4">Recent Downloads</h2>
                <div className="space-y-3">
                    {downloads.slice(0, 5).length === 0 ? (
                        <p className="text-gray-400 text-sm">No recent downloads</p>
                    ) : (
                        downloads.slice(0, 5).map(d => (
                            <div
                                key={d.id}
                                className="flex items-center justify-between p-3 bg-white/5 rounded-lg hover:bg-white/10 transition-all"
                            >
                                <div>
                                    <div className="font-semibold">{d.title}</div>
                                    <div className="text-xs text-gray-400">{d.author} • {d.date}</div>
                                </div>
                                <span className="bg-blue-500/20 px-2 py-1 rounded text-sm">{d.format}</span>
                            </div>
                        ))
                    )}
                </div>
            </div>
        </div>
    )
}
