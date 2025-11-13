'use client'

import React from 'react'
import { useRouter } from 'next/navigation'
import { BarChart3, Settings, Users, FileText, LogOut, Menu, X } from 'lucide-react'

interface SidebarProps {
    activeTab: string
    setActiveTab: (tab: string) => void
    sidebarOpen: boolean
    setSidebarOpen: (open: boolean) => void
}

export default function Sidebar({ activeTab, setActiveTab, sidebarOpen, setSidebarOpen }: SidebarProps) {
    const router = useRouter()

    const menuItems = [
        { id: 'dashboard', label: 'Dashboard', icon: BarChart3 },
        { id: 'downloads', label: 'Downloads', icon: FileText },
        { id: 'users', label: 'Users', icon: Users },
        { id: 'settings', label: 'Settings', icon: Settings },
    ]

    const handleLogout = () => {
        console.log('[Sidebar] Logging out...')

        // Remove token
        localStorage.removeItem('accessToken')
        console.log('[Sidebar] ✓ Token removed')

        // Redirect to home
        console.log('[Sidebar] Redirecting to home...')
        router.push('/')
    }

    return (
        <div className={`${sidebarOpen ? 'w-64' : 'w-20'} bg-white/5 border-r border-white/10 transition-all duration-300 fixed h-screen`}>
            <div className="p-4 border-b border-white/10 flex items-center justify-between">
                {sidebarOpen && (
                    <span className="text-xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
                        YouTubeDown
                    </span>
                )}
                <button
                    onClick={() => setSidebarOpen(!sidebarOpen)}
                    className="p-1 hover:bg-white/10 rounded-lg transition-all"
                >
                    {sidebarOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
                </button>
            </div>

            <nav className="p-4 space-y-2">
                {menuItems.map(item => {
                    const Icon = item.icon
                    return (
                        <button
                            key={item.id}
                            onClick={() => setActiveTab(item.id)}
                            className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-all ${activeTab === item.id
                                    ? 'bg-gradient-to-r from-blue-500 to-cyan-500 text-white'
                                    : 'text-gray-400 hover:bg-white/5'
                                }`}
                        >
                            <Icon className="w-5 h-5 flex-shrink-0" />
                            {sidebarOpen && <span>{item.label}</span>}
                        </button>
                    )
                })}
            </nav>

            <div className="absolute bottom-4 left-4 right-4">
                <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-3 px-4 py-3 rounded-lg text-red-400 hover:bg-red-500/10 transition-all group"
                >
                    <LogOut className="w-5 h-5 flex-shrink-0 group-hover:translate-x-1 transition-transform" />
                    {sidebarOpen && <span>Logout</span>}
                </button>
            </div>
        </div>
    )
}
