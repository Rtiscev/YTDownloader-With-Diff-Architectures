// app/admin/components/StatsCard.tsx
'use client'

import React from 'react'

interface StatsCardProps {
    label: string
    value: string
    icon: string
    color: string
}

export default function StatsCard({ label, value, icon, color }: StatsCardProps) {
    return (
        <div className="p-6 rounded-2xl bg-white/5 border border-white/10 hover:border-white/20 transition-all group">
            <div className="flex items-center justify-between mb-4">
                <span className="text-3xl">{icon}</span>
                <div className={`w-12 h-12 rounded-lg bg-gradient-to-br ${color} opacity-20`}></div>
            </div>
            <div className="text-gray-400 text-sm mb-1">{label}</div>
            <div className="text-3xl font-bold">{value}</div>
        </div>
    )
}
