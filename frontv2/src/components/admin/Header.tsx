// app/admin/components/Header.tsx
'use client'

import React from 'react'

export default function Header() {
    return (
        <div className="border-b border-white/10 bg-white/5 backdrop-blur-md sticky top-0 z-40">
            <div className="p-6 flex items-center justify-between">
                <h1 className="text-3xl font-bold">Admin Panel</h1>
                <div className="flex items-center gap-4">
                    <span className="text-sm text-gray-400">Welcome back, Admin</span>
                </div>
            </div>
        </div>
    )
}
