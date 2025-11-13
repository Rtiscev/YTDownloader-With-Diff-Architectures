// app/api/admin/downloads/route.ts
import { NextRequest, NextResponse } from 'next/server'

// Helper function to verify authentication
function verifyAuth(request: NextRequest): { valid: boolean; error?: string } {
    const authHeader = request.headers.get('authorization')

    if (!authHeader) {
        console.warn('[Admin API] ✗ Missing authorization header')
        return { valid: false, error: 'Unauthorized' }
    }

    if (!authHeader.startsWith('Bearer ')) {
        console.warn('[Admin API] ✗ Invalid authorization header format')
        return { valid: false, error: 'Invalid authorization format' }
    }

    const token = authHeader.split(' ')[1]
    if (!token) {
        console.warn('[Admin API] ✗ No token provided')
        return { valid: false, error: 'No token provided' }
    }

    console.log('[Admin API] ✓ Authorization header valid')
    return { valid: true }
}

// GET - List all downloads
export async function GET(request: NextRequest) {
    console.log('[Admin API] GET /downloads request received')

    // ✓ CHECK 1: Verify authentication at API route level
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        console.warn('[Admin API] ✗ Authentication failed:', authCheck.error)
        return NextResponse.json(
            { error: authCheck.error },
            { status: 401 }
        )
    }

    try {
        const bucketName = 'my-bucket'
        const authHeader = request.headers.get('authorization')

        console.log('[Admin API] Calling MinIO service...')

        // ✓ PASS TOKEN: Forward the token to backend service
        const listResponse = await fetch(
            `http://apigateway:5000/api/minioservice/list/${bucketName}`,
            {
                method: 'GET',
                headers: {
                    'Authorization': authHeader || '',  // ← Pass token here
                    'Content-Type': 'application/json',
                },
                cache: 'no-store',
            }
        )

        console.log('[Admin API] MinIO response status:', listResponse.status)

        if (!listResponse.ok) {
            if (listResponse.status === 401) {
                console.error('[Admin API] ✗ Unauthorized from backend (401)')
                return NextResponse.json(
                    { error: 'Unauthorized' },
                    { status: 401 }
                )
            }
            throw new Error(`MinIO list error: ${listResponse.status}`)
        }

        const files: string[] = await listResponse.json()
        console.log('[Admin API] ✓ Retrieved', files.length, 'files')

        const downloads = files.map((fileName) => {
            const lastDotIndex = fileName.lastIndexOf('.')
            const extension = lastDotIndex !== -1
                ? fileName.slice(lastDotIndex + 1).toLowerCase()
                : 'unknown'

            const nameWithoutExt = lastDotIndex !== -1
                ? fileName.slice(0, lastDotIndex)
                : fileName

            const lastOpenBracket = nameWithoutExt.lastIndexOf('[')
            const lastCloseBracket = nameWithoutExt.lastIndexOf(']')

            const quality = (lastOpenBracket !== -1 && lastCloseBracket !== -1 && lastCloseBracket > lastOpenBracket)
                ? nameWithoutExt.slice(lastOpenBracket + 1, lastCloseBracket).trim()
                : 'Unknown'

            const titleWithAuthor = lastOpenBracket !== -1
                ? nameWithoutExt.slice(0, lastOpenBracket).trim()
                : nameWithoutExt.trim()

            return {
                id: fileName,
                title: titleWithAuthor,
                author: 'Unknown',
                format: extension.toUpperCase(),
                size: 'N/A',
                date: new Date().toISOString().split('T')[0],
                downloads: 0,
                fileName: fileName,
                quality: quality,
            }
        })

        console.log('[Admin API] ✓ Successfully returned downloads')
        return NextResponse.json(downloads, { status: 200 })
    } catch (error) {
        console.error('[Admin API] Error fetching downloads:', error)
        return NextResponse.json(
            { error: 'Failed to fetch downloads' },
            { status: 500 }
        )
    }
}

// DELETE - Delete a file from MinIO
export async function DELETE(request: NextRequest) {
    console.log('[Admin API] DELETE request received')

    // ✓ CHECK 1: Verify authentication at API route level
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        console.warn('[Admin API] ✗ Authentication failed:', authCheck.error)
        return NextResponse.json(
            { error: authCheck.error },
            { status: 401 }
        )
    }

    try {
        const { searchParams } = new URL(request.url)
        const fileName = searchParams.get('fileName')
        const bucketName = 'my-bucket'
        const authHeader = request.headers.get('authorization')

        console.log('[Admin API] DELETE - fileName:', fileName)
        console.log('[Admin API] DELETE - bucketName:', bucketName)

        if (!fileName) {
            console.warn('[Admin API] DELETE failed: fileName is missing')
            return NextResponse.json(
                { error: 'fileName is required' },
                { status: 400 }
            )
        }

        const deleteUrl = `http://apigateway:5000/api/minioservice/delete/${bucketName}/${encodeURIComponent(fileName)}`
        console.log('[Admin API] DELETE - Sending to:', deleteUrl)

        // ✓ PASS TOKEN: Forward the token to backend service
        const deleteResponse = await fetch(deleteUrl, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader || '',  // ← Pass token here
            },
        })

        console.log('[Admin API] DELETE - MinIO response status:', deleteResponse.status)

        if (!deleteResponse.ok) {
            if (deleteResponse.status === 401) {
                console.error('[Admin API] ✗ Unauthorized from backend (401)')
                return NextResponse.json(
                    { error: 'Unauthorized' },
                    { status: 401 }
                )
            }

            const errorData = await deleteResponse.json().catch(() => ({}))
            console.error('[Admin API] DELETE - MinIO error:', errorData)
            throw new Error(errorData.error || `Delete failed with status ${deleteResponse.status}`)
        }

        const result = await deleteResponse.json()
        console.log('[Admin API] ✓ File deleted successfully:', fileName)

        return NextResponse.json(
            { success: true, message: `File "${fileName}" deleted successfully` },
            { status: 200 }
        )
    } catch (error) {
        console.error('[Admin API] DELETE error:', error)
        console.error('[Admin API] Error stack:', error instanceof Error ? error.stack : 'No stack trace')
        return NextResponse.json(
            { error: error instanceof Error ? error.message : 'Failed to delete file' },
            { status: 500 }
        )
    }
}
