'use client'

import React, { useState, useEffect } from 'react'
import { Download, Music, Video, Sparkles, Search, Eye, Clock, Check, Lock, Settings } from 'lucide-react'
import Link from 'next/link'

interface VideoData {
    title: string
    author: string
    thumbnail: string
    views: number
    duration: string
    authorIcon: string
    videoFormats?: Array<{
        id: string
        resolution: string
        filesize: number
    }>
}

interface FormatOption {
    id: string
    label: string
    size: string
    bitrate: string
    quality: string
    resolution?: string
    locked?: boolean
}

export default function HomeComponent() {
    const [url, setUrl] = useState('')
    const [mediaType, setMediaType] = useState<'audio' | 'video'>('audio')
    const [isLoading, setIsLoading] = useState(false)
    const [isSearching, setIsSearching] = useState(false)
    const [isQualityDropdownOpen, setIsQualityDropdownOpen] = useState(false)
    const [videoData, setVideoData] = useState<VideoData | null>(null)
    const [formatOptions, setFormatOptions] = useState<FormatOption[]>([])
    const [selectedFormat, setSelectedFormat] = useState<FormatOption | null>(null)
    const [videoTitle, setVideoTitle] = useState('')
    const [authenticated, setAuthenticated] = useState(false)
    const [firstName, setFirstName] = useState<string | null>(null)
    const [isAdmin, setIsAdmin] = useState(false)

    useEffect(() => {
        checkAuth()
    }, [])

    const checkAuth = async () => {
        console.log('[Home] Checking authentication...')
        const token = localStorage.getItem('accessToken')

        if (!token) {
            console.log('[Home] No token found')
            setAuthenticated(false)
            setFirstName(null)
            setIsAdmin(false)
            return
        }

        try {
            const res = await fetch('/api/auth/me', {
                method: 'GET',
                headers: { Authorization: `Bearer ${token}` },
            })

            console.log(res)
            if (res.ok) {
                const user = await res.json()
                console.log(user)
                console.log('[Home] ✓ User authenticated:', user.firstName)
                setAuthenticated(true)
                setFirstName(user.firstName)
                // --- Admin Role check ---
                let admin = false
                if (user.role) {
                    admin = user.role === 'Admin' 
                } else if (Array.isArray(user.roles)) {
                    admin = user.roles.includes('Admin')
                }
                setIsAdmin(admin)
            } else {
                console.log('[Home] Authentication failed, status:', res.status)
                setAuthenticated(false)
                setFirstName(null)
                setIsAdmin(false)
                if (res.status === 401) localStorage.removeItem('accessToken')
            }
        } catch (err) {
            console.error('[Home] Auth check error:', err)
            setAuthenticated(false)
            setFirstName(null)
            setIsAdmin(false)
        }
    }

    const handleLogout = () => {
        console.log('[Home] Logging out...')
        localStorage.removeItem('accessToken')
        setAuthenticated(false)
        setFirstName(null)
    }

    const handleSearch = async () => {
        if (!url.trim()) return

        console.log('[Home] Searching for video:', url)
        setIsSearching(true)

        try {
            const res = await fetch(`/api/yt/info?url=${encodeURIComponent(url)}`)

            if (!res.ok) throw new Error('Failed to fetch video info')

            const data = await res.json()

            if (!data.success) {
                console.error('[Home] API error:', data.errorMessage)
                return
            }

            console.log('[Home] ✓ Video found:', data.title)
            setVideoTitle(data.title)
            setVideoData({
                title: data.title,
                author: data.channel,
                thumbnail: data.thumbnailUrl,
                views: data.viewCount,
                duration: formatDuration(data.duration),
                authorIcon: data.thumbnailUrl,
                videoFormats: data.videoFormats,
            })

            const audioFormats: FormatOption[] = [
                { id: '96k', label: '96 kbps MP3', size: 'CBR 96kbps', bitrate: '96k', quality: 'Low', locked: false },
                { id: '128k', label: '128 kbps MP3', size: 'CBR 128kbps', bitrate: '128k', quality: 'Medium', locked: false },
                { id: '192k', label: '192 kbps MP3', size: 'CBR 192kbps', bitrate: '192k', quality: 'High', locked: true },
                { id: '320k', label: '320 kbps MP3', size: 'CBR 320kbps', bitrate: '320k', quality: 'Best', locked: true },
            ]

            setFormatOptions(audioFormats)
            setSelectedFormat(audioFormats[0])
            setMediaType('audio')
        } catch (err) {
            console.error('[Home] Search error:', err)
        } finally {
            setIsSearching(false)
        }
    }

    const formatDuration = (seconds: number): string => {
        const mins = Math.floor(seconds / 60)
        const secs = seconds % 60
        return `${mins}:${secs.toString().padStart(2, '0')}`
    }

    const formatFileSize = (bytes: number | null): string => {
        if (!bytes) return 'Unknown'
        const mb = bytes / 1024 / 1024
        return `${mb.toFixed(1)} MB`
    }

    const getQualityLabel = (resolution: string): string => {
        const parts = resolution.split('x')
        if (parts.length !== 2) return 'Low'

        const height = parseInt(parts[1])

        if (height >= 2160) return '4K'
        if (height >= 1440) return '2K'
        if (height >= 1080) return 'Full HD'
        if (height >= 720) return 'HD'
        if (height >= 480) return 'SD'
        if (height >= 360) return '360p'
        if (height >= 240) return '240p'

        return 'Low'
    }

    const handleDownload = async () => {
        if (!url.trim() || !selectedFormat) {
            console.warn('[Home] Download aborted: Missing URL or format')
            return
        }

        console.log('[Home] Download initiated')
        console.log('[Home] Selected format locked:', selectedFormat.locked)
        console.log('[Home] User authenticated:', authenticated)

        if (selectedFormat.locked && !authenticated) {
            console.warn('[Home] ✗ Attempt to download premium quality without authentication')
            alert('🔒 Premium quality download requires login!\n\nPlease sign in to access High, Best quality audio and HD+ video formats.')
            return
        }

        setIsLoading(true)

        try {
            const downloadRequest = {
                url,
                format: mediaType === 'video' ? selectedFormat.id : undefined,
                extractAudio: mediaType === 'audio',
                audioFormat: mediaType === 'audio' ? 'mp3' : undefined,
                audioQuality: mediaType === 'audio' ? selectedFormat.id : undefined,
                resolution: mediaType === 'video' ? selectedFormat.resolution : undefined,
                mergeAudio: mediaType === 'video',
            }

            const qualityLabel = mediaType === 'audio'
                ? selectedFormat.id
                : selectedFormat.resolution

            const expectedFileName = `${videoTitle} [${qualityLabel}]${mediaType === 'audio' ? '.mp3' : '.mp4'}`

            console.log('[Home] Download request:', {
                quality: qualityLabel,
                authenticated,
                fileName: expectedFileName,
            })

            const checkResponse = await fetch('/api/yt/check-file', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    url,
                    qualityLabel,
                    mediaType,
                    fileName: expectedFileName,
                }),
            })

            if (checkResponse.ok) {
                const checkData = await checkResponse.json()

                if (checkData.exists) {
                    console.log('[Home] ✓ File already exists, skipping download')
                    triggerDownload({
                        success: true,
                        fileName: checkData.fileName,
                        downloadUrl: checkData.downloadUrl,
                        message: 'File already exists - skipped download',
                    })
                    setIsLoading(false)
                    return
                }
            }

            const token = localStorage.getItem('accessToken')
            console.log('[Home] Sending download request, authenticated:', !!token)

            const downloadResponse = await fetch('/api/yt/download-and-upload', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...(token && { 'Authorization': `Bearer ${token}` }),
                },
                body: JSON.stringify(downloadRequest),
            })

            if (downloadResponse.status === 401) {
                console.warn('[Home] ✗ Unauthorized - premium quality requires login')
                alert('🔒 This quality requires authentication!\n\nPlease login to download premium quality.')
                setIsLoading(false)
                return
            }

            if (!downloadResponse.ok) {
                const errorData = await downloadResponse.json()
                console.error('[Home] Download failed:', errorData)
                alert(`Download failed: ${errorData.error || downloadResponse.statusText}`)
                setIsLoading(false)
                return
            }

            const result = await downloadResponse.json()

            if (!result.success) {
                console.error('[Home] Download failed:', result.error)
                alert(`Download failed: ${result.error}`)
                setIsLoading(false)
                return
            }

            console.log('[Home] ✓ Download successful')
            triggerDownload(result)
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Unknown error'
            console.error('[Home] Download error:', message)
            alert(`Download error: ${message}`)
        } finally {
            setIsLoading(false)
        }
    }

    const triggerDownload = (result: any) => {
        console.log('[Home] Triggering download for:', result.fileName)
        const link = document.createElement('a')
        link.href = result.downloadUrl
        link.download = result.fileName || 'download'
        document.body.appendChild(link)
        link.click()

        setTimeout(() => {
            document.body.removeChild(link)
        }, 100)
    }

    const formatViews = (views: number) => {
        if (views >= 1000000) return (views / 1000000).toFixed(1) + 'M'
        if (views >= 1000) return (views / 1000).toFixed(1) + 'K'
        return views.toString()
    }

    return (
        <div className="min-h-screen bg-black text-white overflow-hidden">
            {/* Background */}
            <div className="fixed inset-0 -z-10">
                <div className="absolute inset-0 bg-gradient-to-br from-slate-900 via-black to-slate-900"></div>
                <div className="absolute top-0 left-1/4 w-96 h-96 bg-blue-600/20 rounded-full blur-3xl opacity-60 animate-pulse"></div>
                <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-cyan-600/20 rounded-full blur-3xl opacity-60 animate-pulse" style={{ animationDelay: '1s' }}></div>
                <div className="absolute -top-1/2 right-0 w-96 h-96 bg-purple-600/10 rounded-full blur-3xl opacity-40"></div>
                <div className="absolute top-1/2 -left-1/2 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl opacity-40"></div>
            </div>

            <div className="relative z-10">
                {/* Navigation */}
                <nav className="backdrop-blur-md bg-white/5 border-b border-white/10 sticky top-0 z-50">
                    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            {/* ...Logo/Brand... */}
                        </div>
                        <div className="flex items-center gap-4">
                            <div className="hidden md:flex items-center gap-6">
                                <a href="#download" className="text-sm text-gray-300 hover:text-white transition-colors">Download</a>
                                <a href="#features" className="text-sm text-gray-300 hover:text-white transition-colors">Features</a>
                            </div>
                            {authenticated ? (
                                <>
                                    <span className="px-4 py-2 rounded-lg bg-green-600/50 text-white font-semibold">
                                        Welcome, <span className="font-bold">{firstName || 'User'}</span>
                                    </span>
                                    {/* --- SHOW ONLY IF ADMIN --- */}
                                    {isAdmin && (
                                        <Link
                                            href="/admin"
                                            className="px-4 py-2 rounded-lg bg-blue-600 text-white font-semibold hover:bg-blue-700 transition flex items-center gap-2"
                                        >
                                            <Settings className="w-4 h-4" />
                                            Admin
                                        </Link>
                                    )}
                                    <button
                                        onClick={handleLogout}
                                        className="px-4 py-2 rounded-lg bg-red-600 text-white font-semibold hover:bg-red-700 transition"
                                    >
                                        Logout
                                    </button>
                                </>
                            ) : (
                                <>
                                    <Link href="/login" className="px-4 py-2 rounded-lg bg-white/10 text-blue-400 font-semibold hover:bg-blue-500 hover:text-white transition">
                                        Login
                                    </Link>
                                    <Link href="/signup" className="px-4 py-2 rounded-lg bg-gradient-to-r from-cyan-400 to-blue-500 text-white font-semibold hover:shadow-lg transition">
                                        Signup
                                    </Link>
                                </>
                            )}
                        </div>
                    </div>
                </nav>

                {/* Hero Section */}
                <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-16 pb-12 md:pt-24 md:pb-16">
                    <div className="text-center mb-8 md:mb-12">
                        <div className="inline-flex items-center gap-2 mb-6 px-4 py-2 rounded-full bg-white/5 border border-white/10 backdrop-blur-md">
                            <Sparkles className="w-4 h-4 text-cyan-400" />
                            <span className="text-sm font-medium text-gray-300">Download YouTube content instantly</span>
                        </div>
                        <h1 className="text-4xl md:text-6xl lg:text-7xl font-black mb-4 leading-tight">
                            <span className="bg-gradient-to-r from-blue-400 via-cyan-400 to-blue-400 bg-clip-text text-transparent">
                                Convert & Download
                            </span>
                            <br />
                            <span className="text-gray-100">Your YouTube Videos</span>
                        </h1>
                        <p className="text-base md:text-lg text-gray-400 max-w-2xl mx-auto mb-6">
                            High-quality audio and video downloads in your preferred format. Fast, free, and secure.
                        </p>
                    </div>
                </section>

                {/* Main Downloader Section */}
                <section id="download" className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 mb-12">
                    <div className="backdrop-blur-xl bg-white/5 border border-white/10 rounded-3xl p-8 md:p-10 shadow-2xl">
                        {/* URL Input */}
                        <div className="mb-8">
                            <label className="block text-sm font-semibold text-gray-300 mb-3">YouTube URL</label>
                            <div className="flex gap-3">
                                <div className="flex-1 relative group">
                                    <input
                                        type="text"
                                        value={url}
                                        onChange={(e) => setUrl(e.target.value)}
                                        placeholder="https://youtube.com/watch?v=..."
                                        className="w-full px-6 py-4 rounded-xl bg-white/5 border-2 border-white/10 focus:border-cyan-500 focus:outline-none transition-all placeholder-gray-500 font-medium text-white group-hover:border-white/20"
                                    />
                                    <div className="absolute inset-0 rounded-xl bg-gradient-to-r from-blue-500 to-cyan-500 opacity-0 group-hover:opacity-10 transition-all pointer-events-none"></div>
                                </div>
                                <button
                                    onClick={handleSearch}
                                    disabled={!url.trim() || isSearching}
                                    className="group relative px-8 py-4 rounded-xl bg-gradient-to-r from-blue-500 to-cyan-500 hover:from-blue-600 hover:to-cyan-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold transition-all shadow-lg hover:shadow-cyan-500/50 flex items-center gap-2 overflow-hidden"
                                >
                                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white to-transparent opacity-0 group-hover:opacity-20 -translate-x-full group-hover:translate-x-full transition-all duration-500"></div>
                                    <Search className="w-5 h-5 relative z-10" />
                                    <span className="relative z-10">{isSearching ? 'Searching...' : 'Search'}</span>
                                </button>
                            </div>
                        </div>

                        {/* Video Preview */}
                        {videoData && (
                            <>
                                <div className="mb-6 rounded-2xl overflow-hidden bg-black/50 border border-white/10 backdrop-blur-md group">
                                    <div className="relative aspect-video overflow-hidden">
                                        <img
                                            src={videoData.thumbnail}
                                            alt={videoData.title}
                                            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                                        />
                                        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent"></div>
                                        <div className="absolute bottom-4 right-4 bg-black/90 backdrop-blur px-3 py-1 rounded-lg text-sm font-semibold">
                                            {videoData.duration}
                                        </div>
                                        <div className="absolute bottom-0 left-0 right-0 p-6 bg-gradient-to-t from-black via-black/80 to-transparent">
                                            <h3 className="text-2xl font-bold text-white mb-3 line-clamp-2">{videoData.title}</h3>
                                            <div className="flex items-center justify-between">
                                                <div className="flex items-center gap-3">
                                                    <img
                                                        src={videoData.authorIcon}
                                                        alt={videoData.author}
                                                        className="w-12 h-12 rounded-full object-cover border-2 border-white/20"
                                                    />
                                                    <span className="font-semibold text-white">{videoData.author}</span>
                                                </div>
                                                <div className="flex items-center gap-3 text-xs">
                                                    <div className="flex items-center gap-1 bg-white/10 px-2 py-1 rounded backdrop-blur">
                                                        <Eye className="w-3 h-3 text-cyan-400" />
                                                        <span className="text-gray-200">{formatViews(videoData.views)}</span>
                                                    </div>
                                                    <div className="flex items-center gap-1 bg-white/10 px-2 py-1 rounded backdrop-blur">
                                                        <Clock className="w-3 h-3 text-cyan-400" />
                                                        <span className="text-gray-200">{videoData.duration}</span>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                {/* Download Options */}
                                <div className="space-y-4">
                                    {/* Format Selection */}
                                    <div>
                                        <label className="block text-xs font-semibold text-gray-400 mb-2 uppercase tracking-wide">Format</label>
                                        <div className="flex gap-2">
                                            <button
                                                onClick={() => {
                                                    setMediaType('audio')
                                                    setSelectedFormat(null)
                                                    setIsQualityDropdownOpen(false)

                                                    const audioFormats: FormatOption[] = [
                                                        { id: '96k', label: '96 kbps MP3', size: 'CBR 96kbps', bitrate: '96k', quality: 'Low', locked: false },
                                                        { id: '128k', label: '128 kbps MP3', size: 'CBR 128kbps', bitrate: '128k', quality: 'Medium', locked: false },
                                                        { id: '192k', label: '192 kbps MP3', size: 'CBR 192kbps', bitrate: '192k', quality: 'High', locked: true },
                                                        { id: '320k', label: '320 kbps MP3', size: 'CBR 320kbps', bitrate: '320k', quality: 'Best', locked: true },
                                                    ]

                                                    setFormatOptions(audioFormats)
                                                    setSelectedFormat(audioFormats.find(f => !f.locked) || audioFormats[0])
                                                }}
                                                className={`flex-1 py-2.5 px-3 rounded-lg font-semibold transition-all text-sm flex items-center justify-center gap-2 ${mediaType === 'audio'
                                                    ? 'bg-gradient-to-r from-blue-500 to-cyan-500 text-white shadow-lg shadow-cyan-500/50'
                                                    : 'bg-white/5 border border-white/10 text-gray-300 hover:bg-white/10'
                                                    }`}
                                            >
                                                <Music className="w-4 h-4" />
                                                <span>MP3</span>
                                            </button>

                                            <button
                                                onClick={() => {
                                                    setMediaType('video')
                                                    setSelectedFormat(null)
                                                    setIsQualityDropdownOpen(false)

                                                    if (videoData?.videoFormats) {
                                                        const videoFormatsOptions = videoData.videoFormats.map((fmt) => {
                                                            const height = parseInt(fmt.resolution.split('x')[1])
                                                            const isLocked = height > 480

                                                            return {
                                                                id: fmt.id,
                                                                label: `${fmt.resolution} MP4`,
                                                                size: formatFileSize(fmt.filesize),
                                                                bitrate: 'H.264',
                                                                quality: getQualityLabel(fmt.resolution),
                                                                resolution: fmt.resolution,
                                                                locked: isLocked,
                                                            }
                                                        })

                                                        setFormatOptions(videoFormatsOptions)
                                                        setSelectedFormat(videoFormatsOptions.find(f => !f.locked) || videoFormatsOptions[0])
                                                    }
                                                }}
                                                className={`flex-1 py-2.5 px-3 rounded-lg font-semibold transition-all text-sm flex items-center justify-center gap-2 ${mediaType === 'video'
                                                    ? 'bg-gradient-to-r from-blue-500 to-cyan-500 text-white shadow-lg shadow-cyan-500/50'
                                                    : 'bg-white/5 border border-white/10 text-gray-300 hover:bg-white/10'
                                                    }`}
                                            >
                                                <Video className="w-4 h-4" />
                                                <span>MP4</span>
                                            </button>
                                        </div>
                                    </div>

                                    {/* Quality Selection */}
                                    <div className="relative">
                                        <label className="block text-xs font-semibold text-gray-400 mb-2 uppercase tracking-wide">Quality</label>
                                        <button
                                            onClick={() => setIsQualityDropdownOpen(!isQualityDropdownOpen)}
                                            className="w-full flex items-center justify-between p-3 rounded-lg bg-white/5 border-2 border-white/10 hover:border-white/20 text-white font-semibold transition-all"
                                        >
                                            <span>{selectedFormat?.quality || 'Select quality...'}</span>
                                            <span className={`transform transition-transform ${isQualityDropdownOpen ? 'rotate-180' : ''}`}>▼</span>
                                        </button>

                                        {isQualityDropdownOpen && (
                                            <div className="absolute top-full left-0 right-0 mt-2 bg-slate-900 border border-white/20 rounded-lg z-50 shadow-xl">
                                                {formatOptions.map((fmt, index) => (
                                                    <button
                                                        key={fmt.id}
                                                        onClick={() => {
                                                            if (fmt.locked && !authenticated) {
                                                                console.warn('[Home] Locked quality clicked by unauthenticated user')
                                                                alert('🔒 Login required!\n\nThis premium quality requires authentication.')
                                                                return
                                                            }
                                                            setSelectedFormat(fmt)
                                                            setIsQualityDropdownOpen(false)
                                                        }}
                                                        disabled={fmt.locked && !authenticated}
                                                        className={`w-full flex items-center justify-between p-3 hover:bg-slate-800 transition-all ${index !== formatOptions.length - 1 ? 'border-b border-white/10' : ''
                                                            } ${selectedFormat?.id === fmt.id ? 'bg-blue-500/30' : ''} ${fmt.locked && !authenticated ? 'opacity-50 cursor-not-allowed' : ''
                                                            }`}
                                                    >
                                                        <div className="text-left">
                                                            <div className="font-semibold text-sm flex items-center gap-2">
                                                                {fmt.quality}
                                                                {fmt.locked && !authenticated && (
                                                                    <Lock className="w-3 h-3 text-yellow-400" />
                                                                )}
                                                            </div>
                                                            <div className="text-xs text-gray-400">{fmt.label}</div>
                                                        </div>
                                                        <div className="flex items-center gap-2">
                                                            <div className="text-right text-xs text-gray-400">
                                                                <div>{fmt.size}</div>
                                                            </div>
                                                            {selectedFormat?.id === fmt.id && !fmt.locked && (
                                                                <Check className="w-4 h-4 text-cyan-400 flex-shrink-0" />
                                                            )}
                                                        </div>
                                                    </button>
                                                ))}
                                            </div>
                                        )}
                                    </div>

                                    {/* Download Button */}
                                    <button
                                        onClick={handleDownload}
                                        disabled={!selectedFormat || isLoading}
                                        className="w-full group relative py-3 px-6 rounded-lg bg-gradient-to-r from-blue-500 via-cyan-500 to-blue-500 hover:from-blue-600 hover:via-cyan-600 hover:to-blue-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-bold transition-all shadow-2xl hover:shadow-cyan-500/50 hover:scale-[1.02] flex items-center justify-center gap-2 overflow-hidden mt-2"
                                    >
                                        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white to-transparent opacity-0 group-hover:opacity-20 -translate-x-full group-hover:translate-x-full transition-all duration-500"></div>
                                        <Download className="w-5 h-5 relative z-10" />
                                        <span className="relative z-10 font-semibold">
                                            {isLoading ? 'Downloading...' : 'Download'}
                                        </span>
                                    </button>
                                </div>
                            </>
                        )}

                        {/* Empty State */}
                        {!videoData && (
                            <div className="text-center py-16">
                                <div className="w-16 h-16 rounded-full bg-white/5 border border-white/10 flex items-center justify-center mx-auto mb-4">
                                    <Search className="w-8 h-8 text-gray-500" />
                                </div>
                                <p className="text-gray-400 font-medium">Paste a YouTube URL to get started</p>
                            </div>
                        )}
                    </div>
                </section>

                {/* Features Section */}
                <section id="features" className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
                    <h2 className="text-4xl md:text-5xl font-bold text-center mb-12">Why Choose Us?</h2>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        {[
                            { icon: '⚡', title: 'Lightning Fast', desc: 'Download in seconds' },
                            { icon: '🔒', title: 'Secure & Private', desc: 'No data stored or logged' },
                            { icon: '🎯', title: 'High Quality', desc: 'Multiple format options' },
                            { icon: '🌍', title: 'Universal Support', desc: 'Works with any YouTube video' },
                            { icon: '📱', title: 'Cross Platform', desc: 'Desktop and mobile friendly' },
                            { icon: '⚙️', title: 'Advanced Options', desc: 'Choose your perfect quality' },
                        ].map((feature, i) => (
                            <div key={i} className="p-6 rounded-xl bg-white/5 border border-white/10 hover:bg-white/10 transition-all group cursor-pointer">
                                <div className="text-4xl mb-3 group-hover:scale-110 transition-transform">{feature.icon}</div>
                                <h3 className="text-xl font-bold mb-2 group-hover:text-cyan-400 transition-colors">{feature.title}</h3>
                                <p className="text-gray-400">{feature.desc}</p>
                            </div>
                        ))}
                    </div>
                </section>

                {/* Footer */}
                <footer className="border-t border-white/10 mt-20 py-8 text-center text-gray-400">
                    <p>© 2025 YouTubeDown. Download responsibly and respect copyright laws.</p>
                </footer>
            </div>
        </div>
    )
}
