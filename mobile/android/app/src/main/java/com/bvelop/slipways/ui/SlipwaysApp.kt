package com.bvelop.slipways.ui

import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.consumeWindowInsets
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Anchor
import androidx.compose.material.icons.filled.DirectionsBoat
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Water
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraphBuilder
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.navigation
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.bvelop.slipways.ui.details.SlipwayDetailsScreen
import com.bvelop.slipways.ui.info.InfoScreen
import com.bvelop.slipways.ui.overview.MarinaOverviewScreen
import com.bvelop.slipways.ui.overview.SlipwayOverviewScreen
import com.bvelop.slipways.ui.waters.WaterDetailsScreen
import com.bvelop.slipways.ui.waters.WaterOverviewScreen

/** Detail routes carry the tab as prefix, so a slipway opened from a water stays in the waters tab. */
private object Routes {
    const val SLIPWAYS_TAB = "slipways"
    const val MARINAS_TAB = "marinas"
    const val WATERS_TAB = "waters"
    const val INFO_TAB = "info"

    fun list(tab: String) = "$tab/list"
    fun slipway(tab: String) = "$tab/slipway/{id}"
    fun water(tab: String) = "$tab/water/{id}"
}

private enum class Tab(val route: String, val label: String, val icon: ImageVector) {
    SLIPWAYS(Routes.SLIPWAYS_TAB, "Slipanlagen", Icons.Filled.DirectionsBoat),
    MARINAS(Routes.MARINAS_TAB, "Marinas", Icons.Filled.Anchor),
    WATERS(Routes.WATERS_TAB, "Gewässer", Icons.Filled.Water),
    INFO(Routes.INFO_TAB, "Info", Icons.Filled.Info),
}

@Composable
fun SlipwaysApp() {
    val navController = rememberNavController()
    val backStackEntry by navController.currentBackStackEntryAsState()
    val idArgument = listOf(navArgument("id") { type = NavType.StringType })

    Scaffold(
        // Each screen has its own top bar and handles the status bar itself.
        contentWindowInsets = WindowInsets(0, 0, 0, 0),
        bottomBar = {
            NavigationBar {
                Tab.entries.forEach { tab ->
                    NavigationBarItem(
                        selected = backStackEntry?.destination?.hierarchy?.any { it.route == tab.route } == true,
                        onClick = {
                            navController.navigate(tab.route) {
                                popUpTo(navController.graph.findStartDestination().id) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        },
                        icon = { Icon(tab.icon, contentDescription = null) },
                        label = { Text(tab.label) },
                    )
                }
            }
        },
    ) { padding ->
        NavHost(
            navController = navController,
            startDestination = Routes.SLIPWAYS_TAB,
            modifier = Modifier.padding(padding).consumeWindowInsets(padding),
        ) {
            val back: () -> Unit = { navController.popBackStack() }

            // Slipways, marinas and waters link to each other, so every tab knows both detail screens.
            fun NavGraphBuilder.tab(tab: String, list: @Composable (openSlipway: (String) -> Unit, openWater: (String) -> Unit) -> Unit) {
                val openSlipway = { id: String -> navController.navigate("$tab/slipway/$id") }
                val openWater = { id: String -> navController.navigate("$tab/water/$id") }
                navigation(startDestination = Routes.list(tab), route = tab) {
                    composable(Routes.list(tab)) { list(openSlipway, openWater) }
                    composable(Routes.slipway(tab), arguments = idArgument) {
                        SlipwayDetailsScreen(onBack = back, onWaterClick = openWater, onSlipwayClick = openSlipway)
                    }
                    composable(Routes.water(tab), arguments = idArgument) {
                        WaterDetailsScreen(onBack = back, onSlipwayClick = openSlipway, onWaterClick = openWater)
                    }
                }
            }

            tab(Routes.SLIPWAYS_TAB) { openSlipway, _ -> SlipwayOverviewScreen(onSlipwayClick = openSlipway) }
            tab(Routes.MARINAS_TAB) { openSlipway, openWater -> MarinaOverviewScreen(onWaterClick = openWater, onSlipwayClick = openSlipway) }
            tab(Routes.WATERS_TAB) { _, openWater -> WaterOverviewScreen(onWaterClick = openWater) }
            navigation(startDestination = Routes.list(Routes.INFO_TAB), route = Routes.INFO_TAB) {
                composable(Routes.list(Routes.INFO_TAB)) { InfoScreen() }
            }
        }
    }
}
