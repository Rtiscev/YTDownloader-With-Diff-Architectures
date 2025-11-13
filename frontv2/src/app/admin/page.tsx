'use client'

import React, { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import Sidebar from '@/components/admin/Sidebar'
import Header from '@/components/admin/Header'
import DashboardTab from '@/components/admin/DashboardTab'
import DownloadsTab from '@/components/admin/DownloadsTab'
import UsersTab from '@/components/admin/UsersTab'
import SettingsTab from '@/components/admin/SettingsTab'

export interface DownloadRecord {
    id: string
    title: string
    author: string
    format: string
    size: string
    date: string
    downloads: number
    fileName: string
    quality: string
}

export default function AdminPanel() {
    const router = useRouter()
    const [activeTab, setActiveTab] = useState('dashboard')
    const [sidebarOpen, setSidebarOpen] = useState(true)
    const [downloads, setDownloads] = useState<DownloadRecord[]>([])
    const [loading, setLoading] = useState(true)
    const [authenticated, setAuthenticated] = useState(false)
    const [isAdmin, setIsAdmin] = useState<boolean | null>(null)
    const [authChecked, setAuthChecked] = useState(false)

    useEffect(() => {
        checkAuth()
    }, [])

    useEffect(() => {
        if (authenticated && isAdmin) {
            console.log('[AdminPanel] ✓ Fetching downloads...')
            fetchDownloads()
        }
    }, [authenticated, isAdmin])

    const checkAuth = async () => {
        console.log('[AdminPanel] Checking authentication...')

        const token = localStorage.getItem('accessToken')
        if (!token) {
            console.warn('[AdminPanel] ✗ No token found')
            setAuthenticated(false)
            setIsAdmin(false)
            setAuthChecked(true)
            return
        }

        try {
            const res = await fetch('/api/auth/me', {
                method: 'GET',
                headers: { Authorization: `Bearer ${token}` },
            })

            if (res.ok) {
                const user = await res.json()
                console.log('[AdminPanel] User:', user.email)

                let admin = false
                if (user.role) {
                    admin = user.role === 'Admin'
                } else if (Array.isArray(user.roles)) {
                    admin = user.roles.includes('Admin')
                }

                console.log('[AdminPanel] Is admin:', admin)
                setAuthenticated(true)
                setIsAdmin(admin)
                setAuthChecked(true)
            } else {
                console.warn('[AdminPanel] ✗ Auth failed:', res.status)
                setAuthenticated(false)
                setIsAdmin(false)
                setAuthChecked(true)
                if (res.status === 401) {
                    localStorage.removeItem('accessToken')
                }
            }
        } catch (err) {
            console.error('[AdminPanel] Auth error:', err)
            setAuthenticated(false)
            setIsAdmin(false)
            setAuthChecked(true)
        }
    }

    const fetchDownloads = async () => {
        if (!authenticated || !isAdmin) {
            console.warn('[AdminPanel] ✗ Not authorized to fetch downloads')
            setLoading(false)
            return
        }

        try {
            console.log('[AdminPanel] Fetching downloads...')
            setLoading(true)

            const token = localStorage.getItem('accessToken')
            if (!token) {
                console.error('[AdminPanel] ✗ No token available')
                setLoading(false)
                return
            }

            const response = await fetch('/api/admin/downloads', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                },
            })

            if (response.status === 401) {
                console.error('[AdminPanel] ✗ Token invalid')
                localStorage.removeItem('accessToken')
                setAuthenticated(false)
                router.push('/login')
                return
            }

            if (response.status === 403) {
                console.error('[AdminPanel] ✗ Forbidden')
                setIsAdmin(false)
                return
            }

            if (!response.ok) {
                throw new Error(`Failed to fetch downloads: ${response.status}`)
            }

            const data = await response.json()
            console.log('[AdminPanel] ✓ Downloads loaded:', data.length)
            setDownloads(data)
        } catch (err) {
            console.error('[AdminPanel] Fetch error:', err)
        } finally {
            setLoading(false)
        }
    }

    if (!authChecked) {
        return (
            <div className="min-h-screen bg-black text-white flex items-center justify-center">
                <div className="text-center">
                    <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-cyan-400 mb-4"></div>
                    <p className="text-gray-400">Checking authentication...</p>
                </div>
            </div>
        )
    }

    if (!isAdmin) {
        return (
            <div className="min-h-screen bg-black text-white flex items-center justify-center">
                <div className="text-center">
                    <div className="text-6xl mb-4">⛔</div>
                    <h1 className="text-3xl font-bold mb-2">Forbidden</h1>
                    <p className="text-gray-400 mb-6">You must be an <b>admin</b> to access the admin panel.</p>
                    <button
                        onClick={() => router.push('/')}
                        className="px-6 py-2 bg-gradient-to-r from-blue-500 to-cyan-500 rounded-lg font-semibold hover:shadow-lg hover:shadow-cyan-500/50 transition-all"
                    >
                        Go to Home
                    </button>
                </div>
            </div>
        )
    }

    return (
        <div className="min-h-screen bg-black text-white">
            <div className="fixed inset-0 -z-10 bg-gradient-to-br from-slate-900 via-black to-slate-900">
                <div className="absolute top-0 left-1/4 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl opacity-40"></div>
                <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-cyan-600/10 rounded-full blur-3xl opacity-40"></div>
            </div>

            <div className="flex relative z-10">
                <Sidebar
                    activeTab={activeTab}
                    setActiveTab={setActiveTab}
                    sidebarOpen={sidebarOpen}
                    setSidebarOpen={setSidebarOpen}
                />

                <div className={`flex-1 ${sidebarOpen ? 'ml-64' : 'ml-20'} transition-all duration-300`}>
                    <Header />

                    <div className="p-6 space-y-6">
                        {activeTab === 'dashboard' && <DashboardTab downloads={downloads} />}
                        {activeTab === 'downloads' && <DownloadsTab />}
                        {activeTab === 'users' && <UsersTab />}
                        {activeTab === 'settings' && <SettingsTab />}
                    </div>
                </div>
            </div>
        </div>
    )
}
