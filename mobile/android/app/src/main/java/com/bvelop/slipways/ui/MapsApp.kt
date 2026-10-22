package com.bvelop.slipways.ui

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.widget.Toast
import androidx.core.net.toUri

/** Hands a place to a map app: with a route there, or just to look at. */
object MapsApp {
    fun route(context: Context, latitude: Double, longitude: Double, name: String) {
        // Google Maps starts the route right away; other map apps get the place.
        if (!open(context, "google.navigation:q=$latitude,$longitude".toUri())) show(context, latitude, longitude, name)
    }

    fun show(context: Context, latitude: Double, longitude: Double, name: String) {
        open(context, "geo:0,0?q=$latitude,$longitude(${Uri.encode(name)})".toUri(), report = true)
    }

    /** False when no app handles the link. */
    private fun open(context: Context, uri: Uri, report: Boolean = false): Boolean =
        try {
            context.startActivity(Intent(Intent.ACTION_VIEW, uri))
            true
        } catch (_: ActivityNotFoundException) {
            if (report) Toast.makeText(context, "Keine Karten-App installiert.", Toast.LENGTH_SHORT).show()
            false
        }
}
