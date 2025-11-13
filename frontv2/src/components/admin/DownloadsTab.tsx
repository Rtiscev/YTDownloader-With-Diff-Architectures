'use client'

import React, { useState, useEffect } from 'react'
import { Trash2, Search, RefreshCw } from 'lucide-react'

interface DownloadRecord {
    author: string
    date: string
    downloads: number
    fileName: string
    format: string
    id: string
    quality: string
    size: string
    title: string
}

export default function DownloadsTab() {
    const [downloads, setDownloads] = useState<DownloadRecord[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)
    const [searchQuery, setSearchQuery] = useState('')
    const [deletingId, setDeletingId] = useState<string | null>(null)

    useEffect(() => {
        fetchDownloads()
    }, [])

    const getAuthHeaders = () => {
        const token = localStorage.getItem('accessToken')
        if (!token) throw new Error('Authentication required. Please login.')
        return {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
        }
    }

    const handleAuthError = (status: number): string | null => {
        if (status === 401) {
            localStorage.removeItem('accessToken')
            return 'Session expired. Please login again.'
        }
        if (status === 403) {
            return 'You do not have permission to perform this action.'
        }
        return null
    }

    const fetchDownloads = async () => {
        try {
            console.log('[DownloadsTab] Fetching downloads...')
            setLoading(true)
            setError(null)

            const response = await fetch('/api/admin/downloads', {
                method: 'GET',
                headers: getAuthHeaders(),
            })

            const authError = handleAuthError(response.status)
            if (authError) {
                setError(authError)
                setLoading(false)
                return
            }

            if (!response.ok) {
                throw new Error(`Failed to fetch downloads (${response.status})`)
            }

            const data = await response.json()
            console.log('[DownloadsTab] ✓ Loaded:', data.length, 'records')
            setDownloads(data)
        } catch (err) {
            console.error('[DownloadsTab] Error:', err)
            setError(err instanceof Error ? err.message : 'Failed to load downloads')
        } finally {
            setLoading(false)
        }
    }

    const handleDelete = async (fileName: string) => {
        if (!confirm(`Delete "${fileName}"?`)) return

        try {
            console.log('[DownloadsTab] Deleting:', fileName)
            setDeletingId(fileName)

            const response = await fetch(
                `/api/admin/downloads?fileName=${encodeURIComponent(fileName)}`,
                {
                    method: 'DELETE',
                    headers: getAuthHeaders(),
                }
            )

            const authError = handleAuthError(response.status)
            if (authError) {
                alert(authError)
                setDeletingId(null)
                return
            }

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}))
                throw new Error(errorData.error || `Delete failed (${response.status})`)
            }

            console.log('[DownloadsTab] ✓ Deleted:', fileName)
            setDownloads(prev => prev.filter(d => d.fileName !== fileName))
            alert('File deleted successfully!')
        } catch (err) {
            console.error('[DownloadsTab] Delete error:', err)
            alert(err instanceof Error ? err.message : 'Failed to delete file')
        } finally {
            setDeletingId(null)
        }
    }

    const filteredDownloads = downloads.filter(d =>
        d.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        d.author.toLowerCase().includes(searchQuery.toLowerCase()) ||
        d.quality.toLowerCase().includes(searchQuery.toLowerCase()) ||
        d.format.toLowerCase().includes(searchQuery.toLowerCase())
    )

    if (loading) {
        return (
            <div className="flex items-center justify-center p-12">
                <div className="text-center">
                    <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-2 text-cyan-400" />
                    <p className="text-gray-400">Loading downloads...</p>
                </div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="p-6 rounded-2xl bg-red-500/10 border border-red-500/20">
                <p className="text-red-400">Error: {error}</p>
                <button
                    onClick={fetchDownloads}
                    className="mt-4 px-4 py-2 bg-red-500/20 rounded-lg hover:bg-red-500/30 transition-all"
                >
                    Retry
                </button>
            </div>
        )
    }

    return (
        <div className="space-y-4">
            <div className="flex items-center gap-4">
                <div className="flex-1 relative">
                    <Search className="absolute left-3 top-3 w-5 h-5 text-gray-500" />
                    <input
                        type="text"
                        placeholder="Search downloads..."
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 bg-white/5 border border-white/10 rounded-lg focus:border-cyan-500 focus:outline-none transition-all text-white"
                    />
                </div>
                <button
                    onClick={fetchDownloads}
                    disabled={loading}
                    className="flex items-center gap-2 px-4 py-2 bg-white/10 rounded-lg hover:bg-white/20 transition-all disabled:opacity-50"
                >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                    Refresh
                </button>
            </div>

            <div className="text-sm text-gray-400">
                Showing {filteredDownloads.length} of {downloads.length} downloads
            </div>

            <div className="rounded-2xl overflow-hidden border border-white/10 bg-white/5">
                <table className="w-full">
                    <thead>
                        <tr className="border-b border-white/10">
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Title</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Quality</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Format</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Date</th>
                            <th className="px-6 py-3 text-left text-sm font-semibold text-gray-400">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filteredDownloads.length === 0 ? (
                            <tr>
                                <td colSpan={5} className="px-6 py-8 text-center text-gray-400">
                                    No downloads found
                                </td>
                            </tr>
                        ) : (
                            filteredDownloads.map(d => (
                                <tr key={d.id} className="border-b border-white/10 hover:bg-white/5 transition-all">
                                    <td className="px-6 py-4 text-sm max-w-md truncate" title={d.title}>
                                        {d.title}
                                    </td>
                                    <td className="px-6 py-4 text-sm">
                                        <span className="bg-purple-500/20 px-2 py-1 rounded text-xs">
                                            {d.quality}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-sm">
                                        <span className="bg-blue-500/20 px-2 py-1 rounded text-xs">
                                            {d.format}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-400">{d.date}</td>
                                    <td className="px-6 py-4 text-sm">
                                        <button
                                            onClick={() => handleDelete(d.fileName)}
                                            disabled={deletingId === d.fileName}
                                            className="p-1.5 hover:bg-red-500/10 rounded transition-all disabled:opacity-50 disabled:cursor-not-allowed group"
                                            title="Delete file"
                                        >
                                            {deletingId === d.fileName ? (
                                                <RefreshCw className="w-4 h-4 text-red-400 animate-spin" />
                                            ) : (
                                                <Trash2 className="w-4 h-4 text-red-400 group-hover:text-red-300" />
                                            )}
                                        </button>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    )
}
